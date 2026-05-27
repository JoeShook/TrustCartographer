using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using TrustCartographer.UI.Live;

namespace TrustCartographer.UI.Pages;

public partial class LiveFlow : ComponentBase, IDisposable
{
    private readonly List<TrafficEntry> entries = new();
    private readonly HashSet<int> openIndexes = new();
    private bool isRunning;
    private string? errorMessage;

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

    private async Task TriggerIssuanceAsync()
    {
        if (isRunning) return;
        isRunning = true;
        errorMessage = null;
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

            // 2. Hand the offer to the Holder wallet (through the proxy).
            var redeemResp = await http.PostAsJsonAsync("holder/wallet/redeem-offer", new { offerUri });
            redeemResp.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            errorMessage = $"Issuance failed: {ex.Message}";
        }
        finally
        {
            isRunning = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ClearLog()
    {
        TrafficLog.Clear();
        entries.Clear();
        openIndexes.Clear();
    }

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
