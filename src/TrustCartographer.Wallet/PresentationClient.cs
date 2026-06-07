using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TrustCartographer.Core.Oid4Vp;
using TrustCartographer.Core.SdJwt;

namespace TrustCartographer.Wallet;

/// <summary>
/// Drives the OID4VP 1.0 presentation flow on behalf of the wallet:
///   parse request → (dereference request_uri) → pick a matching credential →
///   build a holder-signed VP bound to the verifier's nonce/audience →
///   direct_post the vp_token → return the verifier's verdict.
/// Each HTTP hop is a separate call so the proxy logs every step.
/// </summary>
public sealed class PresentationClient
{
    private readonly HttpClient _http;
    private readonly HolderKeyMaterial _keys;
    private readonly WalletStore _store;
    private readonly VpService _vp;
    private readonly SdJwtService _sdJwt;
    private readonly ILogger<PresentationClient> _log;

    public PresentationClient(
        HttpClient http,
        HolderKeyMaterial keys,
        WalletStore store,
        VpService vp,
        SdJwtService sdJwt,
        ILogger<PresentationClient> log)
    {
        _http = http;
        _keys = keys;
        _store = store;
        _vp = vp;
        _sdJwt = sdJwt;
        _log = log;
    }

    public async Task<PresentationOutcome> PresentAsync(string requestUri, CancellationToken ct = default)
    {
        // 1. Resolve the authorization request (by-reference is what the demo uses).
        PresentationRequest request;
        var byRef = PresentationRequestUri.ParseByReference(requestUri);
        if (byRef is not null)
        {
            request = await _http.GetFromJsonAsync<PresentationRequest>(byRef, ct)
                ?? throw new InvalidOperationException("Verifier returned an empty request object.");
        }
        else
        {
            request = PresentationRequestUri.ParseByValue(requestUri)
                ?? throw new InvalidOperationException("Unrecognized presentation request URI.");
        }

        // 2. Select a held credential satisfying the definition.
        var required = request.PresentationDefinition.RequiredCredentialType;
        var credential = _store.All().FirstOrDefault(c => c.CredentialConfigurationId == required)
            ?? throw new InvalidOperationException(
                $"No credential of type '{required}' is held by this wallet. Redeem an offer first.");

        // 3. Build the presentation. An SD-JWT credential (contains '~') is presented by
        //    revealing only the requested disclosures + a Key Binding JWT; a plain JWT-VC is
        //    wrapped in a holder-signed VP-JWT.
        string vpToken;
        if (credential.Jwt.Contains(SdJwtConstants.Delimiter))
        {
            var (issuerJwt, disclosures) = _sdJwt.ParseIssued(credential.Jwt);
            var wanted = request.PresentationDefinition.RequiredClaims ?? Array.Empty<string>();
            var chosen = disclosures.Where(d => wanted.Contains(d.ClaimName)).ToList();
            vpToken = _sdJwt.Present(issuerJwt, chosen, _keys.KeyPair.PrivateKey, request.ClientId, request.Nonce);
        }
        else
        {
            vpToken = _vp.BuildHolderVpJwt(
                holderPrivateKey: _keys.KeyPair.PrivateKey,
                holderPublicKey: _keys.KeyPair.PublicKey,
                holderDid: _keys.Did,
                audience: request.ClientId,
                nonce: request.Nonce,
                vcJwt: credential.Jwt);
        }

        // 4. direct_post the vp_token back to the verifier's response_uri.
        var resp = await _http.PostAsJsonAsync(
            request.ResponseUri,
            new { vp_token = vpToken, state = request.State },
            ct);
        resp.EnsureSuccessStatusCode();

        var outcome = await resp.Content.ReadFromJsonAsync<PresentationOutcome>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Verifier returned an empty response.");

        _log.LogInformation("Wallet presented {Type} to {Verifier} → {Status}",
            required, request.ClientId, outcome.Status);
        return outcome;
    }
}

public record PresentationOutcome(
    string Status,
    string Reason,
    string? HolderDid,
    string? IssuerDid,
    string? CredentialType);
