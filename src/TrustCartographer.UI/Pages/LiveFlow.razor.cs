using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using TrustCartographer.UI.Live;
using TrustCartographer.UI.Shared;

namespace TrustCartographer.UI.Pages;

public partial class LiveFlow : ComponentBase, IDisposable
{
    // ── Captured traffic (raw log) ──
    private readonly List<TrafficEntry> entries = new();
    private readonly HashSet<int> openIndexes = new();
    private bool isRunning;
    private string? errorMessage;

    // ── Animated sequence diagram state (mirrors PractitionerLicense) ──
    private readonly IReadOnlyList<SsiActor> actors = new[]
    {
        new SsiActor
        {
            Key = "browser", Label = "Browser", Sublabel = "demo trigger",
            Tooltip = "This Live Flow page, acting as the operator that kicks off the flow. " +
                      "In a real deployment offers/requests would be QR codes scanned by the wallet."
        },
        new SsiActor
        {
            Key = "issuer", Label = "State Medical Board", Sublabel = "Issuer",
            Tooltip = "OID4VCI credential issuer. Mints offers and issues JWT-VCs after a proof-of-possession handshake."
        },
        new SsiActor
        {
            Key = "holder", Label = "Dr. Smith's Wallet", Sublabel = "Holder",
            Tooltip = "Wallet that redeems offers (OID4VCI) and presents credentials (OID4VP)."
        },
        new SsiActor
        {
            Key = "verifier", Label = "Hospital EHR", Sublabel = "Verifier",
            Tooltip = "OID4VP verifier. Requests a presentation and verifies it: holder proof + issuer signature (resolved from the issuer's did:key)."
        },
    };

    private List<SsiStep> steps = new();
    private List<string> stepExplanations = new();
    private int currentIndex = -1;
    private int? selectedStepIndex;
    private bool isComplete;
    private bool success = true;
    private string? failureReason;
    private bool isPlaying;
    private const int PlayStepDelayMs = 1100;

    // Result-bar labels for the most recent run.
    private string successLabel = "CREDENTIAL ISSUED";
    private string failLabel = "ISSUANCE FAILED";

    protected override void OnInitialized()
    {
        entries.AddRange(TrafficLog.Snapshot());
        TrafficLog.OnNewEntry += HandleEntry;
    }

    private void HandleEntry(TrafficEntry entry)
    {
        entries.Add(entry);
        // Marshal to UI thread; Add fires on the YARP middleware's thread.
        _ = InvokeAsync(StateHasChanged);
    }

    // ───────────────────────── Triggers ─────────────────────────

    private Task TriggerIssuanceAsync() =>
        RunAsync("CREDENTIAL ISSUED", "ISSUANCE FAILED", async http =>
        {
            await MintAndRedeemAsync(http);
            return null; // HTTP-2xx success is sufficient for issuance.
        });

    private Task TriggerVerificationAsync() =>
        RunAsync("VERIFIED", "REJECTED", async http => await RequestAndPresentAsync(http));

    private Task RunFullDemoAsync() =>
        RunAsync("VERIFIED", "REJECTED", async http =>
        {
            await MintAndRedeemAsync(http);
            return await RequestAndPresentAsync(http);
        });

    // Portable Identity Assurance: issue a real SD-JWT minor-identity credential, then
    // present ONLY age_over_18 (selective disclosure) to the verifier — all over the proxy.
    private Task RunMinorAgeCheckAsync() =>
        RunAsync("AGE CLAIM VERIFIED", "REJECTED", async http =>
        {
            var offerResp = await http.PostAsJsonAsync("issuer/credential-offer",
                new { credentialConfigurationId = "MinorIdentityCredential" });
            offerResp.EnsureSuccessStatusCode();
            using var offerDoc = JsonDocument.Parse(await offerResp.Content.ReadAsStringAsync());
            var offerUri = offerDoc.RootElement.GetProperty("offerUri").GetString()
                ?? throw new InvalidOperationException("Issuer did not return offerUri.");
            var redeemResp = await http.PostAsJsonAsync("holder/wallet/redeem-offer", new { offerUri });
            redeemResp.EnsureSuccessStatusCode();

            var reqResp = await http.PostAsJsonAsync("verifier/presentation-request",
                new { credentialType = "MinorIdentityCredential", requiredClaims = new[] { "age_over_18" }, format = "vc+sd-jwt" });
            reqResp.EnsureSuccessStatusCode();
            using var reqDoc = JsonDocument.Parse(await reqResp.Content.ReadAsStringAsync());
            var requestUri = reqDoc.RootElement.GetProperty("requestUri").GetString()
                ?? throw new InvalidOperationException("Verifier did not return requestUri.");

            var presentResp = await http.PostAsJsonAsync("holder/wallet/present", new { requestUri });
            presentResp.EnsureSuccessStatusCode();
            using var pDoc = JsonDocument.Parse(await presentResp.Content.ReadAsStringAsync());
            var status = pDoc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
            var reason = pDoc.RootElement.TryGetProperty("reason", out var r) ? r.GetString() : null;
            return (status == "accepted", reason);
        });

    /// <summary>
    /// Shared run harness: clear capture + diagram, run <paramref name="flow"/> against
    /// a loopback client (so every hop is proxied + captured), then snapshot the captured
    /// traffic into animated steps and play. The flow may return an explicit verdict
    /// (used by verification, whose verdict lives in the response body, not the HTTP code);
    /// null means "judge success purely by HTTP status".
    /// </summary>
    private async Task RunAsync(
        string okLabel, string badLabel,
        Func<HttpClient, Task<(bool ok, string? reason)?>> flow)
    {
        if (isRunning || isPlaying) return;
        isRunning = true;
        errorMessage = null;
        successLabel = okLabel;
        failLabel = badLabel;

        // Fresh capture + clean diagram so the animation reflects exactly this run.
        TrafficLog.Clear();
        entries.Clear();
        openIndexes.Clear();
        steps = new();
        stepExplanations = new();
        ResetAnimation();
        await InvokeAsync(StateHasChanged);

        (bool ok, string? reason)? verdict = null;
        try
        {
            var http = HttpClientFactory.CreateClient("loopback");
            http.BaseAddress = new Uri(Nav.BaseUri);
            verdict = await flow(http);
        }
        catch (Exception ex)
        {
            errorMessage = $"Flow failed: {ex.Message}";
        }
        finally
        {
            // Outer calls log on completion; give late entries a moment to land.
            await Task.Delay(150);
            BuildSteps();
            if (verdict is { } v)
            {
                success = v.ok;
                failureReason = v.ok ? null : v.reason;
            }
            isRunning = false;
        }

        await PlayAll();
    }

    private static async Task MintAndRedeemAsync(HttpClient http)
    {
        var offerResp = await http.PostAsync("issuer/credential-offer", JsonContent.Create(new { }));
        offerResp.EnsureSuccessStatusCode();
        using var offerDoc = JsonDocument.Parse(await offerResp.Content.ReadAsStringAsync());
        var offerUri = offerDoc.RootElement.GetProperty("offerUri").GetString()
            ?? throw new InvalidOperationException("Issuer did not return offerUri.");

        var redeemResp = await http.PostAsJsonAsync("holder/wallet/redeem-offer", new { offerUri });
        redeemResp.EnsureSuccessStatusCode();
    }

    private static async Task<(bool ok, string? reason)?> RequestAndPresentAsync(HttpClient http)
    {
        var reqResp = await http.PostAsync("verifier/presentation-request", JsonContent.Create(new { }));
        reqResp.EnsureSuccessStatusCode();
        using var reqDoc = JsonDocument.Parse(await reqResp.Content.ReadAsStringAsync());
        var requestUri = reqDoc.RootElement.GetProperty("requestUri").GetString()
            ?? throw new InvalidOperationException("Verifier did not return requestUri.");

        var presentResp = await http.PostAsJsonAsync("holder/wallet/present", new { requestUri });
        presentResp.EnsureSuccessStatusCode(); // infra errors throw; a rejected verdict is still 200
        using var pDoc = JsonDocument.Parse(await presentResp.Content.ReadAsStringAsync());
        var status = pDoc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
        var reason = pDoc.RootElement.TryGetProperty("reason", out var r) ? r.GetString() : null;
        return (status == "accepted", reason);
    }

    private void ClearLog()
    {
        isPlaying = false;
        TrafficLog.Clear();
        entries.Clear();
        openIndexes.Clear();
        steps = new();
        stepExplanations = new();
        ResetAnimation();
    }

    // ───────────────────────── Build steps from real traffic ─────────────────────────

    private void BuildSteps()
    {
        var captured = TrafficLog.Snapshot();

        var ordered = captured
            .Select((entry, i) => (entry, hop: Classify(entry), i))
            .OrderBy(x => x.hop.Rank).ThenBy(x => x.i)
            .ToList();

        steps = new List<SsiStep>(ordered.Count);
        stepExplanations = new List<string>(ordered.Count);

        foreach (var (entry, hop, _) in ordered)
        {
            var ok = entry.StatusCode is >= 200 and < 300;
            steps.Add(new SsiStep
            {
                FromKey = hop.FromKey,
                ToKey = hop.ToKey,
                Label = hop.Label,
                HttpMethod = entry.Method,
                HttpPath = entry.Path,
                Sublabel = entry.StatusCode is int sc ? $"{sc} · {entry.DurationMs} ms" : null,
                Outcome = ok ? StepOutcome.Success : StepOutcome.Fail,
                PayloadJson = ComposeWireBytes(entry),
                PayloadLabel = "Wire bytes (request + response)"
            });
            stepExplanations.Add(hop.Explanation);
        }

        success = steps.Count > 0 && steps.All(s => s.Outcome != StepOutcome.Fail);
        failureReason = success ? null : "one or more hops returned a non-2xx status";
    }

    private sealed record HopInfo(string FromKey, string ToKey, string Label, string Explanation, int Rank);

    private static HopInfo Classify(TrafficEntry e)
    {
        bool Has(string s) => e.Path.Contains(s, StringComparison.OrdinalIgnoreCase);

        // ── Issuance (OID4VCI) ──
        if (Has("/credential-offer"))
            return new("browser", "issuer", "Mint credential offer",
                "The Live Flow page (acting as operator) asks the <strong>Issuer</strong> to create a " +
                "pre-authorized credential offer. The response carries the <code>offerUri</code> the wallet will redeem.", 0);
        if (Has("/wallet/redeem-offer"))
            return new("browser", "holder", "Hand offer to wallet",
                "The operator passes the <code>offerUri</code> to <strong>Dr. Smith's wallet</strong>. This single call " +
                "triggers the wallet to run the entire OID4VCI dance with the Issuer — the steps that follow.", 1);
        if (Has("openid-credential-issuer"))
            return new("holder", "issuer", "Fetch issuer metadata",
                "The <strong>wallet</strong> dereferences <code>credential_issuer</code> and reads " +
                "<code>/.well-known/openid-credential-issuer</code> to discover the credential, nonce, and " +
                "authorization-server endpoints.", 2);
        if (Has("oauth-authorization-server"))
            return new("holder", "issuer", "Fetch AS metadata",
                "The wallet reads <code>/.well-known/oauth-authorization-server</code> to find the " +
                "<code>token_endpoint</code> for redeeming the pre-authorized code.", 3);
        if (Has("/token"))
            return new("holder", "issuer", "Redeem pre-auth code → access token",
                "The wallet POSTs the <code>pre-authorized_code</code> and receives a short-lived <code>access_token</code>.", 4);
        if (Has("/nonce"))
            return new("holder", "issuer", "Request c_nonce for proof",
                "The wallet requests a fresh <code>c_nonce</code> to sign — proving it controls the holder's private key.", 5);
        if (Has("/credential"))
            return new("holder", "issuer", "Send PoP proof → receive JWT-VC",
                "The wallet sends a proof JWT (signed over the nonce + audience) with its <code>access_token</code>. " +
                "The Issuer verifies the proof and returns the signed <strong>JWT-VC</strong> bound to the holder's <code>did:key</code>.", 6);

        // ── Verification (OID4VP) ── (check the {id} GET + response before the bare request)
        if (Has("/presentation-response"))
            return new("holder", "verifier", "Post Verifiable Presentation (direct_post)",
                "The <strong>wallet</strong> builds a holder-signed VP (the JWT-VC wrapped + bound to the verifier's " +
                "<code>nonce</code> and <code>client_id</code>) and POSTs the <code>vp_token</code>. The <strong>Verifier</strong> " +
                "checks the holder proof, then resolves the issuer's <code>did:key</code> to verify the credential signature.", 13);
        if (Has("/presentation-request/"))
            return new("holder", "verifier", "Fetch presentation request",
                "The wallet dereferences <code>request_uri</code> to read what the Verifier wants: " +
                "<code>nonce</code>, <code>client_id</code>, <code>response_uri</code>, and the required credential type.", 12);
        if (Has("/wallet/present"))
            return new("browser", "holder", "Hand request to wallet",
                "The operator passes the presentation <code>requestUri</code> to the wallet, which runs the OID4VP flow.", 11);
        if (Has("/presentation-request"))
            return new("browser", "verifier", "Build presentation request",
                "The <strong>Verifier</strong> mints a one-time <code>nonce</code> + <code>state</code> and a request URI " +
                "describing the credential it needs (a <code>presentation_definition</code>).", 10);

        var to = e.To is "issuer" or "holder" or "verifier" ? e.To : "issuer";
        return new("browser", to, $"{e.Method} {e.Path}", "Captured exchange.", 99);
    }

    private static string ComposeWireBytes(TrafficEntry e)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"▶ REQUEST  {e.Method} {e.Path}");
        if (!string.IsNullOrEmpty(e.RequestContentType))
            sb.AppendLine($"   content-type: {e.RequestContentType}");
        sb.AppendLine(string.IsNullOrEmpty(e.RequestBody)
            ? "   (no body)"
            : RenderBody(e.RequestBody, e.RequestContentType));
        sb.AppendLine();
        sb.AppendLine($"◀ RESPONSE {e.StatusCode}{(e.ResponseBodyTruncated ? " (truncated)" : "")}");
        if (!string.IsNullOrEmpty(e.ResponseContentType))
            sb.AppendLine($"   content-type: {e.ResponseContentType}");
        sb.Append(string.IsNullOrEmpty(e.ResponseBody)
            ? "   (no body)"
            : RenderBody(e.ResponseBody, e.ResponseContentType));
        return sb.ToString();
    }

    // ───────────────────────── Animation controls ─────────────────────────

    private void ResetAnimation()
    {
        isPlaying = false;
        currentIndex = -1;
        selectedStepIndex = null;
        isComplete = false;
        success = true;
        failureReason = null;
    }

    private void StepForward()
    {
        if (currentIndex >= steps.Count - 1) return;
        currentIndex++;
        selectedStepIndex = currentIndex;
        if (currentIndex == steps.Count - 1)
            isComplete = true;
    }

    private void SelectStep(int idx)
    {
        if (idx < 0 || idx > currentIndex) return;
        selectedStepIndex = idx;
    }

    private async Task PlayAll()
    {
        if (isPlaying || steps.Count == 0) return;
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

    private async Task Replay()
    {
        if (steps.Count == 0 || isPlaying || isRunning) return;
        currentIndex = -1;
        selectedStepIndex = null;
        isComplete = false;
        await PlayAll();
    }

    // ───────────────────────── Raw-log helpers ─────────────────────────

    private void Toggle(int idx)
    {
        if (!openIndexes.Add(idx)) openIndexes.Remove(idx);
    }

    private static string StatusClass(int? status) => status switch
    {
        null => string.Empty,
        >= 200 and < 300 => "live-flow__status--ok",
        >= 300 and < 400 => "live-flow__status--redir",
        >= 400 and < 500 => "live-flow__status--client-err",
        _ => "live-flow__status--server-err"
    };

    private static string RenderBody(string? body, string? contentType)
    {
        if (string.IsNullOrEmpty(body)) return "(empty)";
        if (string.IsNullOrEmpty(contentType) || !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return body;
        try
        {
            using var doc = JsonDocument.Parse(body);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return body;
        }
    }

    public void Dispose() => TrafficLog.OnNewEntry -= HandleEntry;
}
