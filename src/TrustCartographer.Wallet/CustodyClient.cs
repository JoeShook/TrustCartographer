using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using TrustCartographer.Core.SdJwt;

namespace TrustCartographer.Wallet;

/// <summary>
/// Guardian-initiated <b>custody transfer</b>: a credential held on behalf of a minor is
/// re-issued to the minor's <i>own</i> wallet once they can hold it themselves (the proposal's
/// "Phase 2"). The guardian recovers the held SD-JWT's claims, stamps custody provenance, and
/// asks the original issuer to re-issue them — but with assurance <i>preserved</i> (carried
/// forward), not re-derived. The minor wallet then redeems the offer with its own key, so the
/// new credential is key-bound (<c>cnf</c>) to the minor rather than the guardian.
///
/// Every hop is a real HTTP call through the proxy, so custody transfer is Live-Flow-capturable.
/// </summary>
public sealed class CustodyClient
{
    private readonly HttpClient _http;
    private readonly WalletStore _store;
    private readonly HolderKeyMaterial _keys;
    private readonly SdJwtService _sdJwt;
    private readonly ILogger<CustodyClient> _log;

    public CustodyClient(
        HttpClient http,
        WalletStore store,
        HolderKeyMaterial keys,
        SdJwtService sdJwt,
        ILogger<CustodyClient> log)
    {
        _http = http;
        _store = store;
        _keys = keys;
        _sdJwt = sdJwt;
        _log = log;
    }

    public async Task<CustodyTransferResult> TransferCustodyAsync(
        string credentialId, string minorRedeemUrl, CancellationToken ct = default)
    {
        var held = _store.Get(credentialId)
            ?? throw new InvalidOperationException($"No held credential '{credentialId}'.");

        if (!held.Jwt.Contains(SdJwtConstants.Delimiter))
            throw new InvalidOperationException(
                "Only SD-JWT VC credentials can be custody-transferred (the demo re-issues their claims).");

        // 1. Recover the original claims from the held SD-JWT's disclosures.
        var (_, disclosures) = _sdJwt.ParseIssued(held.Jwt);
        var claims = new Dictionary<string, object?>();
        foreach (var d in disclosures)
        {
            try { claims[d.ClaimName] = JsonNode.Parse(d.ValueJson); }
            catch { claims[d.ClaimName] = d.ValueJson; }
        }

        // 2. Stamp custody provenance — the minor now self-holds; record where it came from.
        claims["custody"] = "self";
        claims["prior_custody"] = "guardian";
        claims["custody_transferred_from"] = _keys.Did;
        claims["custody_transferred_at"] = DateTimeOffset.UtcNow.ToString("O");

        // 3. Ask the ORIGINAL issuer to re-issue, preserving (not re-deriving) assurance.
        var offerEndpoint = $"{held.CredentialIssuer.TrimEnd('/')}/credential-offer";
        var offerResp = await _http.PostAsJsonAsync(offerEndpoint, new
        {
            credentialConfigurationId = held.CredentialConfigurationId,
            subjectClaims = claims,
            preserveProvidedAssurance = true
        }, ct);
        if (!offerResp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Re-issuance offer refused: {(int)offerResp.StatusCode} {await offerResp.Content.ReadAsStringAsync(ct)}");

        using var offerDoc = JsonDocument.Parse(await offerResp.Content.ReadAsStringAsync(ct));
        var offerUri = offerDoc.RootElement.TryGetProperty("offerUri", out var u) ? u.GetString() : null;
        if (string.IsNullOrEmpty(offerUri))
            throw new InvalidOperationException("Re-issuance offer did not return an offerUri.");

        // 4. Hand the offer to the MINOR wallet, which redeems it with its own key.
        var redeemResp = await _http.PostAsJsonAsync(minorRedeemUrl, new { offerUri }, ct);
        if (!redeemResp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Minor wallet could not redeem: {(int)redeemResp.StatusCode} {await redeemResp.Content.ReadAsStringAsync(ct)}");

        var minorCredential = await redeemResp.Content.ReadFromJsonAsync<StoredCredential>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Minor wallet returned an empty credential.");

        _log.LogInformation("Custody of {Type} transferred from guardian {Guardian} to minor wallet (new cred {Id})",
            held.CredentialConfigurationId, _keys.Did, minorCredential.Id);

        return new CustodyTransferResult(
            CredentialType: held.CredentialConfigurationId,
            Issuer: held.CredentialIssuer,
            GuardianDid: _keys.Did,
            MinorCredentialId: minorCredential.Id,
            TransferredClaimCount: claims.Count);
    }
}

public record CustodyTransferRequest(string CredentialId, string MinorRedeemUrl);

public record CustodyTransferResult(
    string CredentialType,
    string Issuer,
    string GuardianDid,
    string MinorCredentialId,
    int TransferredClaimCount);
