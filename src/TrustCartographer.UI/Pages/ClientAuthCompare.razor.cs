using Microsoft.AspNetCore.Components;
using TrustCartographer.UI.Shared;

namespace TrustCartographer.UI.Pages;

/// <summary>
/// Conceptual side-by-side of UDAP JWT vs vLEI/ACDC client authentication over the *same*
/// OAuth2 client-credentials → FHIR flow. A toggle swaps only the client assertion (step 2)
/// and the AS trust validation (step 4); everything else is identical — the visual form of the
/// VITAL white paper's "semantically equivalent, different assertion format" claim.
/// Pure simulation (no HTTP); the real UDAP slice is Stage 1 of the sketch.
/// </summary>
public partial class ClientAuthCompare : ComponentBase
{
    private enum AuthMode { Udap, Vlei }

    private AuthMode mode = AuthMode.Udap;

    private IReadOnlyList<SsiActor> actors = Array.Empty<SsiActor>();
    private List<SsiStep> steps = new();
    private List<string> explanations = new();

    private int currentIndex = -1;
    private int? selectedStepIndex;
    private bool isComplete;
    private bool isPlaying;
    private const int PlayDelayMs = 1100;

    protected override void OnInitialized() => BuildFlow();

    private void SetMode(AuthMode m)
    {
        if (mode == m || isPlaying) return;
        mode = m;
        BuildFlow();
        ResetAnimation();
    }

    // ───────────────────────── Flow construction ─────────────────────────

    private void BuildFlow()
    {
        var udap = mode == AuthMode.Udap;

        actors = new[]
        {
            new SsiActor { Key = "client", Label = "Payer A", Sublabel = "FHIR client",
                Tooltip = "The requesting payer's FHIR client — it authenticates to Payer B's token endpoint." },
            new SsiActor { Key = "as", Label = "Payer B", Sublabel = "Authorization Server",
                Tooltip = "The responding payer's OAuth2 Authorization Server (token endpoint) + FHIR resource server." },
            new SsiActor
            {
                Key = "anchor",
                Label = udap ? "Trust Community" : "GLEIF → QVI",
                Sublabel = udap ? "X.509 CA / anchor" : "vLEI root of trust",
                Tooltip = udap
                    ? "UDAP trust community: X.509 trust anchor (CA) the client certificate must chain to."
                    : "vLEI chain of trust: GLEIF root → Qualified vLEI Issuer, the ACDC chain must trace back to."
            },
            new SsiActor { Key = "fhir", Label = "Payer B", Sublabel = "FHIR server",
                Tooltip = "The FHIR resource server that serves Patient data once a valid access token is presented." },
        };

        steps = new();
        explanations = new();

        // 1 — discovery (same)
        Add("client", "as", "Discover AS metadata", "GET", "/.well-known/udap",
            "The client reads the server's UDAP/SSRAA metadata to find the <code>token_endpoint</code>, supported " +
            "scopes, and the trust community. <strong>Identical in both modes.</strong>",
            payloadLabel: "Discovery (well-known)",
            payload: "GET /.well-known/udap\n\n200 OK\n{\n  \"token_endpoint\": \"https://payer-b.example.com/token\",\n  \"scopes_supported\": [\"system/Patient.read\"],\n  \"udap_versions_supported\": [\"1\"]\n}");

        // 2 — build the client assertion (DIVERGES) — local action on the client
        if (udap)
        {
            AddLocal("client", "◆ Build client_assertion (private_key_jwt)",
                "The client signs a JWT with the private key whose <strong>X.509 cert chain</strong> is carried in the " +
                "JOSE <code>x5c</code> header. <code>iss=sub=client_id</code>, <code>aud</code>=token endpoint, " +
                "<code>exp−iat ≤ 300s</code>, and the <code>hl7-b2b</code> object carries <code>purpose_of_use</code>. " +
                "<br/><em>vLEI mode instead:</em> assemble a CESR-encoded IPEX Grant presenting the ACDC credential chain.",
                "client_assertion (UDAP JWT)",
                "{\n  \"_jose\": { \"alg\": \"RS256\", \"x5c\": [\"<Payer A client cert>\", \"<intermediate>\"] },\n" +
                "  \"iss\": \"https://payer-a.example.com/fhir-client\",\n" +
                "  \"sub\": \"https://payer-a.example.com/fhir-client\",\n" +
                "  \"aud\": \"https://payer-b.example.com/token\",\n" +
                "  \"exp\": 1700000300, \"iat\": 1700000000, \"jti\": \"a1b2c3-nonce\",\n" +
                "  \"extensions\": { \"hl7-b2b\": {\n" +
                "    \"version\": \"1\",\n" +
                "    \"organization_id\": \"https://payer-a.example.com\",\n" +
                "    \"purpose_of_use\": [\"urn:oid:2.16.840.1.113883.5.8#HPAYMT\"]\n" +
                "  }}\n}");
        }
        else
        {
            AddLocal("client", "◆ Assemble vLEI presentation (IPEX Grant)",
                "The client assembles a <strong>CESR-encoded IPEX Grant</strong> presenting its <strong>ACDC credential " +
                "chain</strong>: a Legal Entity vLEI plus an <strong>ECR</strong> credential asserting the exchange " +
                "purpose. Each credential is content-addressed by a SAID and signed (Ed25519). " +
                "<br/><em>UDAP mode instead:</em> sign a <code>private_key_jwt</code> whose <code>x5c</code> header carries an X.509 cert chain.",
                "vLEI presentation (IPEX Grant / ACDC) — conceptual",
                "{\n  \"exchange\": \"IPEX/grant\",\n  \"issuee_aid\": \"EPayerA…  (KERI AID)\",\n" +
                "  \"credentials\": [\n" +
                "    { \"type\": \"Legal Entity vLEI\", \"LEI\": \"PAYERA-LEI\", \"issuer\": \"QVI-AID\" },\n" +
                "    { \"type\": \"ECR\", \"purpose_of_use\": \"payer-to-payer (CMS-0057-F)\", \"issuer\": \"QVI-AID | LE\" }\n" +
                "  ],\n  \"chain\": \"ECR → Legal Entity vLEI → QVI cred → GLEIF root\",\n" +
                "  \"proof\": \"CESR signatures (Ed25519); SAIDs (BLAKE3)\"\n}");
        }

        // 3 — token request (same shape)
        Add("client", "as", "Token request (client_credentials)", "POST", "/token",
            "The client POSTs the assertion to the <strong>same token endpoint</strong> with " +
            "<code>grant_type=client_credentials</code>. <strong>Same OAuth2 call in both modes</strong> — only the " +
            "carried assertion format differs.",
            "POST /token (form)",
            udap
                ? "POST /token\ngrant_type=client_credentials\n&scope=system/Patient.read\n&udap=1\n&client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer\n&client_assertion=<signed UDAP JWT>"
                : "POST /token\ngrant_type=client_credentials\n&scope=system/Patient.read\n&client_assertion_type=<vlei-acdc-ipex>\n&client_assertion=<CESR IPEX Grant>");

        // 4 — AS validates trust (DIVERGES)
        if (udap)
        {
            Add("as", "anchor", "◆ Validate cert chain → trust anchor", null, null,
                "The AS verifies the JWT signature with the <code>x5c</code> leaf key, builds and validates the " +
                "<strong>X.509 chain to a trust-community anchor (CA)</strong>, confirms <code>client_id</code> matches a " +
                "SAN URI, and (optionally) checks the registered software statement. " +
                "<br/><em>vLEI mode instead:</em> walk the ACDC chain to the GLEIF root and check KERI key state.",
                "AS trust validation (UDAP)",
                "verify:\n  • JWT signature  ← x5c leaf public key\n  • X.509 chain   → Trust Community anchor (CA)\n  • client_id (iss/sub) == cert SAN URI\n  • software statement registered (optional)\n  • hl7-b2b.purpose_of_use = HPAYMT  → accepted");
        }
        else
        {
            Add("as", "anchor", "◆ Verify ACDC chain → GLEIF root", null, null,
                "The AS verifies the IPEX grant signatures, walks the <strong>ACDC chain via SAIDs</strong> " +
                "(ECR → Legal Entity vLEI → QVI → <strong>GLEIF root</strong>), checks each AID's <strong>KERI key " +
                "state</strong> (witnesses/watchers), confirms the QVI is currently accredited, and reads the ECR " +
                "purpose. <br/><em>UDAP mode instead:</em> validate an X.509 chain to a community CA.",
                "AS trust validation (vLEI)",
                "verify:\n  • IPEX grant signatures (CESR)\n  • ACDC chain via SAIDs:\n      ECR → LE vLEI → QVI → GLEIF root\n  • KERI key state of each AID (KELs / witnesses)\n  • QVI accreditation current (not revoked)\n  • ECR purpose = payer-to-payer  → accepted");
        }

        // 5 — token issued (same)
        Add("as", "client", "Access token issued", null, null,
            "Trust established, the AS issues a bearer access token scoped to the request. <strong>Identical in both modes.</strong>",
            "Token response",
            "200 OK\n{\n  \"access_token\": \"eyJ… (opaque to the client)\",\n  \"token_type\": \"Bearer\",\n  \"expires_in\": 300,\n  \"scope\": \"system/Patient.read\"\n}");

        // 6 — FHIR call (same)
        Add("client", "fhir", "FHIR request", "GET", "/Patient",
            "The client calls the FHIR API with the bearer token. <strong>Identical in both modes</strong> — the trust " +
            "mechanism is invisible here; it already did its job at the token endpoint.",
            "GET /Patient",
            "GET /Patient\nAuthorization: Bearer eyJ…");

        // 7 — FHIR response (same)
        Add("fhir", "client", "Patient data returned", null, null,
            "The FHIR server returns the resource. The end-to-end outcome is the same; the choice of UDAP vs vLEI changed " +
            "only <em>how the caller proved who it was and what it was allowed to do</em>.",
            "FHIR response",
            "200 OK\n{ \"resourceType\": \"Bundle\", \"type\": \"searchset\", \"entry\": [ … ] }");
    }

    private void Add(string from, string to, string label, string? method, string? path,
        string explanation, string payloadLabel, string payload)
    {
        steps.Add(new SsiStep
        {
            FromKey = from, ToKey = to, Label = label,
            HttpMethod = method, HttpPath = path,
            Outcome = StepOutcome.Success,
            PayloadJson = payload, PayloadLabel = payloadLabel
        });
        explanations.Add(explanation);
    }

    private void AddLocal(string actor, string label, string explanation, string payloadLabel, string payload)
    {
        steps.Add(new SsiStep
        {
            FromKey = actor, ToKey = actor, Label = label, IsLocalAction = true,
            Outcome = StepOutcome.Success, PayloadJson = payload, PayloadLabel = payloadLabel
        });
        explanations.Add(explanation);
    }

    // ───────────────────────── Animation ─────────────────────────

    private void ResetAnimation()
    {
        isPlaying = false;
        currentIndex = -1;
        selectedStepIndex = null;
        isComplete = false;
    }

    private void StepForward()
    {
        if (currentIndex >= steps.Count - 1) return;
        currentIndex++;
        selectedStepIndex = currentIndex;
        if (currentIndex == steps.Count - 1) isComplete = true;
    }

    private void SelectStep(int idx)
    {
        if (idx < 0 || idx > currentIndex) return;
        selectedStepIndex = idx;
    }

    private async Task PlayAll()
    {
        if (isPlaying) return;
        if (currentIndex >= steps.Count - 1) { currentIndex = -1; selectedStepIndex = null; isComplete = false; }
        isPlaying = true;
        try
        {
            while (currentIndex < steps.Count - 1 && isPlaying)
            {
                StepForward();
                await InvokeAsync(StateHasChanged);
                await Task.Delay(PlayDelayMs);
            }
        }
        finally { isPlaying = false; }
    }

    private async Task Replay()
    {
        ResetAnimation();
        await PlayAll();
    }
}
