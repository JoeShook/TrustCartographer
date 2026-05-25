using Microsoft.AspNetCore.Components;
using TrustCartographer.Core.Models;
using TrustCartographer.Core.Services;
using TrustCartographer.UI.Shared;

namespace TrustCartographer.UI.Pages;

public partial class PractitionerLicense : ComponentBase
{
    [Inject] private CryptoService Crypto { get; set; } = null!;
    [Inject] private DidKeyService DidKey { get; set; } = null!;
    [Inject] private VcService Vc { get; set; } = null!;

    private readonly IReadOnlyList<SsiActor> actors = new[]
    {
        new SsiActor
        {
            Key = "issuer", Label = "State Medical Board", Sublabel = "Issuer",
            Tooltip = "Authoritative source for MD licensure. Mints a DID, issues VCs about practitioners."
        },
        new SsiActor
        {
            Key = "holder", Label = "Dr. Smith", Sublabel = "Holder",
            Tooltip = "Receives the VC into her wallet. Presents it to a Verifier on demand."
        },
        new SsiActor
        {
            Key = "verifier", Label = "Hospital EHR", Sublabel = "Verifier",
            Tooltip = "Needs to confirm Dr. Smith is licensed before granting access. Verifies the VC signature."
        },
    };

    private TamperMode tamperMode = TamperMode.None;
    private List<SsiStep> steps = new();
    private List<string> stepExplanations = new();
    private int currentIndex = -1;
    private int? selectedStepIndex;
    private bool isComplete;
    private bool verifySuccess = true;
    private string? failureReason;

    // Per-scenario crypto state (regenerated on Reset / tamper change).
    private KeyPairInfo issuerKeys = null!;
    private string issuerDid = string.Empty;
    private string issuerVmId = string.Empty;
    private KeyPairInfo holderKeys = null!;
    private string holderDid = string.Empty;
    private IssuedJwtVc issuedVc = null!;
    private string presentedJwt = string.Empty;
    private byte[] verifierExpectedIssuerKey = Array.Empty<byte>();
    private VerifyResult? verifyResult;

    protected override void OnInitialized() => Reset();

    private void Reset()
    {
        isPlaying = false; // interrupts any running PlayAll loop
        currentIndex = -1;
        selectedStepIndex = null;
        isComplete = false;
        verifySuccess = true;
        failureReason = null;

        // 1. Issuer mints keys + did:key
        issuerKeys = Crypto.GenerateEd25519KeyPair();
        issuerDid = DidKey.EncodeEd25519(issuerKeys.PublicKey);
        issuerVmId = DidKey.VerificationMethodId(issuerDid);
        var issuerDoc = DidKey.SynthesizeDocument(issuerDid, ["assertionMethod", "authentication"]);

        // 2. Holder mints keys + did:key
        holderKeys = Crypto.GenerateEd25519KeyPair();
        holderDid = DidKey.EncodeEd25519(holderKeys.PublicKey);
        var holderDoc = DidKey.SynthesizeDocument(holderDid, ["authentication"]);

        // 3. Issuer issues VC about Holder
        var claims = new Dictionary<string, object?>
        {
            ["practitionerName"] = "Dr. Jane Smith",
            ["licenseNumber"] = "MD-2026-04891",
            ["specialty"] = "Internal Medicine",
            ["jurisdiction"] = "WA"
        };
        var validity = tamperMode == TamperMode.Expired ? TimeSpan.FromSeconds(-3600) : TimeSpan.FromDays(730);
        issuedVc = Vc.IssueJwtVc(
            issuerDid: issuerDid,
            issuerVerificationMethodId: issuerVmId,
            issuerPrivateKey: issuerKeys.PrivateKey,
            subjectDid: holderDid,
            credentialType: "PractitionerLicenseCredential",
            claims: claims,
            validity: validity);

        // 4. Holder presents (potentially tampered)
        presentedJwt = tamperMode == TamperMode.TamperedSignature
            ? TamperJwtSignature(issuedVc.Jwt)
            : issuedVc.Jwt;

        // 5. Verifier's view of the issuer public key (potentially wrong)
        verifierExpectedIssuerKey = tamperMode == TamperMode.UnknownIssuer
            ? Crypto.GenerateEd25519KeyPair().PublicKey  // verifier knows a DIFFERENT key
            : issuerKeys.PublicKey;

        BuildSteps(issuerDoc, holderDoc);
    }

    private void BuildSteps(string issuerDoc, string holderDoc)
    {
        // Pre-compute verification so we can set the final arrow's outcome.
        verifyResult = Vc.VerifyJwtVc(presentedJwt, verifierExpectedIssuerKey);
        var verifierOutcome = verifyResult.Valid ? StepOutcome.Success : StepOutcome.Fail;

        steps = new List<SsiStep>
        {
            new SsiStep
            {
                FromKey = "issuer", ToKey = "issuer", IsLocalAction = true,
                Label = "Mint did:key (Ed25519)",
                PayloadJson = issuerDoc,
                PayloadLabel = "Issuer DID Document",
                Outcome = StepOutcome.Success
            },
            new SsiStep
            {
                FromKey = "holder", ToKey = "holder", IsLocalAction = true,
                Label = "Mint did:key (Ed25519)",
                PayloadJson = holderDoc,
                PayloadLabel = "Holder DID Document",
                Outcome = StepOutcome.Success
            },
            new SsiStep
            {
                FromKey = "issuer", ToKey = "issuer", IsLocalAction = true,
                Label = "Build VC + sign as JWT",
                PayloadJson = issuedVc.VcBodyPretty,
                PayloadLabel = "Verifiable Credential body (pre-JWT)",
                Outcome = StepOutcome.Success
            },
            new SsiStep
            {
                FromKey = "issuer", ToKey = "holder",
                Label = "Deliver JWT-VC",
                Sublabel = "vc+jwt media type",
                PayloadJwt = issuedVc.Jwt,
                PayloadLabel = "Issued JWT-VC (decoded)",
                Outcome = StepOutcome.Success
            },
            new SsiStep
            {
                FromKey = "verifier", ToKey = "holder",
                Label = "Request presentation",
                Sublabel = "\"prove you're licensed\"",
                HttpMethod = "GET",
                HttpPath = "/verify/practitioner-license",
                Outcome = StepOutcome.Success
            },
            new SsiStep
            {
                FromKey = "holder", ToKey = "verifier",
                Label = "Present JWT-VC",
                HttpMethod = "POST",
                HttpPath = "/vp_token",
                PayloadJwt = presentedJwt,
                PayloadLabel = tamperMode == TamperMode.TamperedSignature
                    ? "Presented JWT-VC (signature tampered)"
                    : "Presented JWT-VC",
                Outcome = tamperMode == TamperMode.TamperedSignature ? StepOutcome.Fail : StepOutcome.Success
            },
            new SsiStep
            {
                FromKey = "verifier", ToKey = "verifier", IsLocalAction = true,
                Label = tamperMode == TamperMode.UnknownIssuer
                    ? "Resolve issuer DID → unknown key"
                    : "Resolve issuer DID → trusted key",
                Outcome = tamperMode == TamperMode.UnknownIssuer ? StepOutcome.Fail : StepOutcome.Success
            },
            new SsiStep
            {
                FromKey = "verifier", ToKey = "verifier", IsLocalAction = true,
                Label = verifyResult.Valid ? "Verify signature ✓" : "Verify signature ✗",
                Outcome = verifierOutcome,
                PayloadJson = BuildVerifyDetailsJson()
            }
        };

        stepExplanations = new List<string>
        {
            "<strong>State Medical Board</strong> generates an Ed25519 key pair and encodes its public key as a " +
            "<code>did:key</code> identifier. The DID literally IS the public key (multicodec + multibase). " +
            "Click the 📨 to inspect the DID Document — note there's no CA, no chain. The Board self-asserts its identity.",

            "<strong>Dr. Smith</strong> independently generates her own Ed25519 key pair and her own <code>did:key</code>. " +
            "Subject identifiers in SSI are typically holder-controlled DIDs; the holder's private key never leaves her wallet.",

            "<strong>The Board</strong> assembles the credential JSON: <code>@context</code>, <code>type</code>, <code>issuer</code>, " +
            "<code>credentialSubject</code> (with Dr. Smith's DID + her licensure claims), <code>validFrom</code>/<code>validUntil</code>. " +
            "It hasn't been signed yet — this is just the data structure.",

            "<strong>The Board</strong> wraps the VC body in a JWT envelope (<code>alg: EdDSA</code>, <code>kid</code> pointing to its " +
            "verification method, <code>typ: vc+jwt</code>), then signs the <code>base64url(header).base64url(payload)</code> string " +
            "with its private key. The full JWT is delivered to Dr. Smith's wallet.",

            "Some time later, <strong>the Hospital EHR</strong> needs proof of licensure before granting access. It sends a presentation " +
            "request to Dr. Smith. (In OID4VP this is a structured <code>presentation_definition</code>; here we keep it conversational.)",

            "<strong>Dr. Smith</strong> returns the JWT-VC. In the happy path she could also wrap it in a signed Verifiable Presentation " +
            "with her own DID — omitted here for clarity. The Verifier now has bytes to check.",

            "<strong>The Verifier</strong> reads the JWT header's <code>kid</code>, extracts the issuer DID, and resolves it. For " +
            "<code>did:key</code> resolution is pure arithmetic: decode the multibase string back to raw Ed25519 public key bytes — " +
            "no network call. (In the <em>Unknown issuer</em> tamper scenario, we simulate the Verifier knowing a different public key, " +
            "as if it doesn't trust the resolved one.)",

            verifyResult.Valid
                ? "<strong>The Verifier</strong> recomputes <code>base64url(header).base64url(payload)</code>, decodes the signature " +
                  "segment, and runs Ed25519 verification with the resolved public key. ✅ Signature valid + <code>exp</code> in the " +
                  "future → credential accepted. Hospital grants access."
                : $"<strong>The Verifier</strong> rejects the credential: <code>{verifyResult.Message}</code>. This is the failure mode " +
                  "you toggled. Try other tamper scenarios to see how each break (tampered bytes vs unknown issuer vs expired) surfaces " +
                  "at this step."
        };
    }

    private string BuildVerifyDetailsJson()
    {
        if (verifyResult == null) return "{}";
        return $$"""
            {
              "valid": {{(verifyResult.Valid ? "true" : "false")}},
              "signatureValid": {{(verifyResult.SignatureValid ? "true" : "false")}},
              "expired": {{(verifyResult.Expired == true ? "true" : verifyResult.Expired == false ? "false" : "null")}},
              "message": "{{verifyResult.Message}}"
            }
            """;
    }

    private static string TamperJwtSignature(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) return jwt;
        var sig = parts[2];
        var firstCh = sig[0];
        var flipped = firstCh == 'A' ? 'B' : 'A';
        return $"{parts[0]}.{parts[1]}.{flipped}{sig[1..]}";
    }

    private void OnTamperChanged(TamperMode mode)
    {
        tamperMode = mode;
        Reset();
    }

    private void StepForward()
    {
        if (currentIndex >= steps.Count - 1) return;
        currentIndex++;
        selectedStepIndex = currentIndex; // selection follows the latest revealed step

        if (currentIndex == steps.Count - 1)
        {
            isComplete = true;
            verifySuccess = verifyResult?.Valid ?? false;
            failureReason = verifySuccess ? null : verifyResult?.Message;
        }
    }

    private void SelectStep(int idx)
    {
        if (idx < 0 || idx > currentIndex) return;
        selectedStepIndex = idx;
    }

    private bool isPlaying;
    private const int PlayStepDelayMs = 1200;

    private async Task PlayAll()
    {
        if (isPlaying) return;
        isPlaying = true;
        try
        {
            while (currentIndex < steps.Count - 1 && isPlaying)
            {
                StepForward();
                await InvokeAsync(StateHasChanged);
                await Task.Delay(PlayStepDelayMs);
            }
        }
        finally
        {
            isPlaying = false;
        }
    }
}
