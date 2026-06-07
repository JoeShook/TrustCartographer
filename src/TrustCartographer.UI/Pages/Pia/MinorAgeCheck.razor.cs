using Microsoft.AspNetCore.Components;
using TrustCartographer.Core.Models;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.SdJwt;
using TrustCartographer.Core.Services;
using TrustCartographer.UI.Shared;

namespace TrustCartographer.UI.Pages.Pia;

public partial class MinorAgeCheck : ComponentBase
{
    [Inject] private CryptoService Crypto { get; set; } = null!;
    [Inject] private DidKeyService DidKey { get; set; } = null!;
    [Inject] private SdJwtService Sd { get; set; } = null!;

    // Step indices whose detail panel renders a richer artifact than plain text.
    private const int StepDeliver = 1;     // SdJwtViewer of the full held credential
    private const int StepPresent = 4;     // SdJwtViewer of the presented (revealed vs withheld) form
    private const int StepVerify = 5;      // verification check table

    private const string VerifierAudience = "https://urgentcare.example/checkin";

    private enum TamperMode { None, RevealEverything, TamperedDisclosure, ReplayedNonce, NoKeyBinding }

    private readonly IReadOnlyList<SsiActor> actors = new[]
    {
        new SsiActor
        {
            Key = "issuer", Label = "Health Identity Issuer", Sublabel = "Issuer (payer / state)",
            Tooltip = "Proofs the guardian and issues the minor's SD-JWT VC, committing salted digests of each claim and binding the holder key via cnf."
        },
        new SsiActor
        {
            Key = "holder", Label = "Guardian Wallet", Sublabel = "Holder",
            Tooltip = "Holds the minor's credential and chooses which claims to reveal. Signs a Key Binding JWT to prove possession."
        },
        new SsiActor
        {
            Key = "verifier", Label = "Urgent Care", Sublabel = "Verifier",
            Tooltip = "Only needs to know whether the patient is over 18 — not their name or date of birth."
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
    private bool isPlaying;
    private const int PlayStepDelayMs = 1200;

    // Per-scenario state (regenerated on Reset / tamper change).
    private IssuedSdJwt issued = null!;
    private string presentation = string.Empty;
    private SdJwtVerifyResult verifyResult = null!;
    private bool privacyLeak;

    protected override void OnInitialized() => Reset();

    private void Reset()
    {
        isPlaying = false;
        currentIndex = -1;
        selectedStepIndex = null;
        isComplete = false;
        verifySuccess = true;
        failureReason = null;
        privacyLeak = false;

        // Issuer + holder key material.
        var issuerKeys = Crypto.GenerateEd25519KeyPair();
        var issuerDid = DidKey.EncodeEd25519(issuerKeys.PublicKey);
        var issuerVmId = DidKey.VerificationMethodId(issuerDid);
        var holderKeys = Crypto.GenerateEd25519KeyPair();
        var holderJwk = Jwk.FromEd25519PublicKey(holderKeys.PublicKey);

        // Issue the minor identity credential (every claim selectively disclosable).
        issued = Sd.IssueSdJwtVc(
            issuerDid: issuerDid,
            issuerVerificationMethodId: issuerVmId,
            issuerPrivateKey: issuerKeys.PrivateKey,
            vct: "https://trustcartographer.example/minor-identity",
            selectivelyDisclosableClaims: new Dictionary<string, object?>
            {
                ["given_name"] = "Alex",
                ["family_name"] = "Rivera",
                ["birthdate"] = "2012-04-01",
                ["age_over_13"] = true,
                ["age_over_18"] = false
            },
            holderCnfJwk: holderJwk,
            validity: TimeSpan.FromDays(365));

        // Holder chooses which disclosures to reveal.
        var chosen = tamperMode == TamperMode.RevealEverything
            ? issued.Disclosures.ToList()
            : issued.Disclosures.Where(d => d.ClaimName == "age_over_18").ToList();
        privacyLeak = tamperMode == TamperMode.RevealEverything;

        var verifierNonce = "n-" + Guid.NewGuid().ToString("N")[..12];
        var presentNonce = tamperMode == TamperMode.ReplayedNonce ? "n-stale-replayed" : verifierNonce;

        // Build the presentation (possibly degraded for a tamper scenario).
        if (tamperMode == TamperMode.NoKeyBinding)
        {
            // Issuance form only — no Key Binding JWT proving holder possession.
            presentation = issued.IssuerJwt + SdJwtConstants.Delimiter +
                           string.Concat(chosen.Select(d => d.Encoded + SdJwtConstants.Delimiter));
        }
        else
        {
            presentation = Sd.Present(issued.IssuerJwt, chosen, holderKeys.PrivateKey, VerifierAudience, presentNonce);
            if (tamperMode == TamperMode.TamperedDisclosure && chosen.Count > 0)
            {
                var enc = chosen[0].Encoded;
                var flipped = enc[..^1] + (enc[^1] == 'A' ? 'B' : 'A');
                presentation = presentation.Replace(enc, flipped);
            }
        }

        verifyResult = Sd.Verify(presentation, issuerKeys.PublicKey, VerifierAudience, verifierNonce,
            requiredClaims: new[] { "age_over_18" });

        BuildSteps(presentNonce, verifierNonce);
    }

    private void BuildSteps(string presentNonce, string verifierNonce)
    {
        var verifyOutcome = verifyResult.Valid ? StepOutcome.Success : StepOutcome.Fail;

        var requestJson =
            $"{{\n  \"nonce\": \"{verifierNonce}\",\n  \"aud\": \"{VerifierAudience}\",\n  \"required\": [\"age_over_18\"]\n}}";

        steps = new List<SsiStep>
        {
            new() { FromKey = "issuer", ToKey = "issuer", IsLocalAction = true,
                Label = "Mint SD-JWT VC (salted digests + cnf)", Outcome = StepOutcome.Success },
            new() { FromKey = "issuer", ToKey = "holder",
                Label = "Deliver credential to guardian wallet", Sublabel = "vc+sd-jwt",
                Outcome = StepOutcome.Success },
            new() { FromKey = "verifier", ToKey = "holder",
                Label = "Request: is the patient over 18?", HttpMethod = "POST", HttpPath = "/presentation-request",
                PayloadJson = requestJson, PayloadLabel = "Presentation request (nonce + required claim)",
                Outcome = StepOutcome.Success },
            new() { FromKey = "holder", ToKey = "holder", IsLocalAction = true,
                Label = tamperMode == TamperMode.RevealEverything
                    ? "Wallet reveals ALL claims (over-disclosure)"
                    : "Wallet reveals age_over_18 only (withholds name, DOB)",
                Outcome = StepOutcome.Success },
            new() { FromKey = "holder", ToKey = "verifier",
                Label = tamperMode == TamperMode.NoKeyBinding ? "Present SD-JWT (no Key Binding)" : "Present SD-JWT + Key Binding JWT",
                HttpMethod = "POST", HttpPath = "/presentation-response",
                Outcome = StepOutcome.Success },
            new() { FromKey = "verifier", ToKey = "verifier", IsLocalAction = true,
                Label = verifyResult.Valid ? "Verify ✓" : "Verify ✗",
                Outcome = verifyOutcome },
        };

        stepExplanations = new List<string>
        {
            "<strong>The Issuer</strong> builds an <strong>SD-JWT VC</strong>: for each claim it generates a random salt, " +
            "forms the disclosure <code>base64url([salt, name, value])</code>, and commits its SHA-256 <code>digest</code> in the " +
            "credential's <code>_sd</code> array. The holder's public key is bound via <code>cnf</code>. Nothing is signed in clear " +
            "except <code>iss</code>, <code>vct</code>, <code>cnf</code>, and the digests — the values live only in the disclosures.",

            "<strong>The Issuer</strong> delivers the issuer-signed JWT plus <em>all</em> disclosures to the guardian wallet " +
            "(<code>jwt~d1~d2~…~</code>). Open the credential below: the wallet holds every claim, but each is independently revealable. " +
            "This is the qualitative difference from an X.509 cert or a SMART Health Card, which expose their whole payload or nothing.",

            "<strong>Urgent Care</strong> asks a single question — <em>is the patient over 18?</em> — and sends a one-time " +
            "<code>nonce</code> and its <code>aud</code> identifier. It requests only the <code>age_over_18</code> claim. (In the real " +
            "OID4VP flow this is a <code>presentation_definition</code>/DCQL query.)",

            tamperMode == TamperMode.RevealEverything
                ? "<strong>The wallet</strong> reveals <em>every</em> disclosure — name, date of birth, everything — even though only " +
                  "<code>age_over_18</code> was asked for. This still verifies cryptographically, but it is an <strong>over-disclosure</strong> " +
                  "privacy failure: the verifier learned far more than it needed."
                : "<strong>The wallet</strong> selects only the <code>age_over_18</code> disclosure and drops the rest. The withheld claims " +
                  "are unrecoverable from the presentation — their digests in <code>_sd</code> are one-way hashes of salted values.",

            tamperMode == TamperMode.NoKeyBinding
                ? "<strong>The wallet</strong> sends the SD-JWT <em>without</em> a Key Binding JWT. Anyone who intercepted the credential " +
                  "could replay it — there's no proof the sender controls the bound holder key."
                : "<strong>The wallet</strong> appends a <strong>Key Binding JWT</strong> signed with the holder's private key, carrying the " +
                  "verifier's <code>aud</code>, the <code>nonce</code>, and <code>sd_hash</code> (a hash over exactly the disclosures being " +
                  "presented). This proves possession and pins the presentation to this verifier and this challenge.",

            verifyResult.Valid
                ? "<strong>Urgent Care</strong> verifies: issuer signature ✓, each revealed disclosure's digest is in <code>_sd</code> ✓, " +
                  "the Key Binding JWT verifies against <code>cnf</code> with matching <code>aud</code>/<code>nonce</code>/<code>sd_hash</code> ✓, " +
                  "and the required <code>age_over_18</code> claim was disclosed ✓. It learns the answer (<code>age_over_18 = false</code>) and " +
                  "nothing else." + (privacyLeak ? " <em>But note the over-disclosure above — verification success does not mean privacy was preserved.</em>" : "")
                : $"<strong>Urgent Care</strong> rejects the presentation: <code>{verifyResult.Message}</code>. This is the failure mode you " +
                  "toggled — switch scenarios to see which check catches each break."
        };
    }

    private void OnTamperChanged(TamperMode mode)
    {
        tamperMode = mode;
        Reset();
    }

    private bool ShowCredentialViewer(int idx) => idx == StepDeliver;
    private bool ShowPresentationViewer(int idx) => idx == StepPresent;
    private bool ShowVerifyTable(int idx) => idx == StepVerify;

    private void StepForward()
    {
        if (currentIndex >= steps.Count - 1) return;
        currentIndex++;
        selectedStepIndex = currentIndex;
        if (currentIndex == steps.Count - 1)
        {
            isComplete = true;
            verifySuccess = verifyResult.Valid;
            failureReason = verifyResult.Valid ? null : verifyResult.Message;
        }
    }

    private void SelectStep(int idx)
    {
        if (idx < 0 || idx > currentIndex) return;
        selectedStepIndex = idx;
    }

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
