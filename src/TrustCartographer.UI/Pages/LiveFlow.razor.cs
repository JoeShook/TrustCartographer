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
            Tooltip = "This Live Flow page, acting as the operator that kicks off issuance. " +
                      "In a real deployment the offer would be shown as a QR code and scanned by the wallet."
        },
        new SsiActor
        {
            Key = "issuer", Label = "State Medical Board", Sublabel = "Issuer",
            Tooltip = "OID4VCI credential issuer. Mints offers and issues JWT-VCs after a proof-of-possession handshake."
        },
        new SsiActor
        {
            Key = "holder", Label = "Dr. Smith's Wallet", Sublabel = "Holder",
            Tooltip = "Wallet that redeems the offer and runs the OID4VCI dance to obtain and store the credential."
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

    // ───────────────────────── Trigger + capture ─────────────────────────

    private async Task TriggerIssuanceAsync()
    {
        if (isRunning || isPlaying) return;
        isRunning = true;
        errorMessage = null;

        // Fresh capture + clean diagram so the animation reflects exactly this run.
        TrafficLog.Clear();
        entries.Clear();
        openIndexes.Clear();
        steps = new();
        stepExplanations = new();
        ResetAnimation();
        await InvokeAsync(StateHasChanged);

        try
        {
            var http = HttpClientFactory.CreateClient("loopback");
            http.BaseAddress = new Uri(Nav.BaseUri);

            // 1. Mint a credential offer on the Issuer (through the proxy).
            var offerResp = await http.PostAsync("issuer/credential-offer", JsonContent.Create(new { }));
            offerResp.EnsureSuccessStatusCode();
            using var offerDoc = JsonDocument.Parse(await offerResp.Content.ReadAsStringAsync());
            var offerUri = offerDoc.RootElement.GetProperty("offerUri").GetString()
                ?? throw new InvalidOperationException("Issuer did not return offerUri.");

            // 2. Hand the offer to the Holder wallet (through the proxy). This single
            //    call drives the whole OID4VCI dance, so every hop is captured.
            var redeemResp = await http.PostAsJsonAsync("holder/wallet/redeem-offer", new { offerUri });
            redeemResp.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            errorMessage = $"Issuance failed: {ex.Message}";
        }
        finally
        {
            // The outer redeem-offer entry logs on completion; give late entries a
            // moment to land before snapshotting them into steps.
            await Task.Delay(150);
            BuildSteps();
            isRunning = false;
        }

        await PlayAll();
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
                "The wallet POSTs the <code>pre-authorized_code</code> (grant type " +
                "<code>urn:ietf:params:oauth:grant-type:pre-authorized_code</code>) and receives a short-lived " +
                "<code>access_token</code>.", 4);
        if (Has("/nonce"))
            return new("holder", "issuer", "Request c_nonce for proof",
                "The wallet requests a fresh <code>c_nonce</code>. It will sign this nonce to prove it controls the " +
                "holder's private key (proof of possession).", 5);
        if (Has("/credential"))
            return new("holder", "issuer", "Send PoP proof → receive JWT-VC",
                "The wallet sends a proof JWT (signed over the nonce + audience) with its <code>access_token</code>. " +
                "The Issuer verifies the proof and returns the signed <strong>JWT-VC</strong> bound to the holder's " +
                "<code>did:key</code>.", 6);

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
