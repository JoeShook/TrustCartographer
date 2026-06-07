using System.Net;
using System.Net.Http.Json;
using TrustCartographer.Core.Proofing;
using TrustCartographer.Issuing;

namespace TrustCartographer.CspIssuer.Services;

/// <summary>
/// Pathway C assurance: instead of a fixed level, this issuer <b>consults the Identity-CSP</b>.
/// At offer time it fetches the referenced proofing event through the proxy, honours its
/// issuable/floor decision, and embeds the CSP-derived effective IDIAL (plus the proofing event
/// id) into the credential so the delegated-proofing basis travels with it. The HTTP hop goes
/// through the same proxy that captures Live Flow traffic, so the CSP appears as its own lifeline.
/// </summary>
public sealed class CspAssuranceSource : IAssuranceSource
{
    private readonly HttpClient _http;
    private readonly ILogger<CspAssuranceSource> _log;

    public CspAssuranceSource(HttpClient http, ILogger<CspAssuranceSource> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<AssuranceResolution> ResolveAsync(AssuranceResolutionContext context, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(context.ProofingEventId))
            return AssuranceResolution.Refuse(
                "Pathway C requires a proofing_event_id from the Identity-CSP. Run a delegated proofing on the CSP first.");

        var url = $"{context.ProxyBaseUrl.TrimEnd('/')}/identity-csp/proofing-event/{Uri.EscapeDataString(context.ProofingEventId)}";

        ProofingEvent? ev;
        try
        {
            var resp = await _http.GetAsync(url, ct);
            if (resp.StatusCode == HttpStatusCode.NotFound)
                return AssuranceResolution.Refuse($"Identity-CSP has no proofing event '{context.ProofingEventId}'.");
            resp.EnsureSuccessStatusCode();
            ev = await resp.Content.ReadFromJsonAsync<ProofingEvent>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not consult Identity-CSP proofing event {Id}", context.ProofingEventId);
            return AssuranceResolution.Refuse($"Could not consult Identity-CSP: {ex.Message}");
        }

        if (ev is null)
            return AssuranceResolution.Refuse("Identity-CSP returned an empty proofing event.");
        if (!ev.Issuable)
            return AssuranceResolution.Refuse($"Proofing event is not issuable: {ev.Reason}");

        var claims = new Dictionary<string, object?>
        {
            ["assurance_level"] = FormatIdial(ev.MinorEffectiveIdial),
            ["assurance_pathway"] = "C-ial2-csp",
            ["assurance_method"] = ev.Method,
            ["assurance_basis"] = ev.Reason,
            ["proofing_event_id"] = ev.Id
        };
        return AssuranceResolution.Allow(claims);
    }

    private static string FormatIdial(IdialLevel level) =>
        level == IdialLevel.Unproofed ? "UNPROOFED" : $"IDIAL{(int)level}";
}
