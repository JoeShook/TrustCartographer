using System.Net.Http.Json;
using TrustCartographer.Core.Oid4Vci;

namespace TrustCartographer.Holder.Services;

/// <summary>
/// Drives the OID4VCI 1.0 pre-authorized code flow on behalf of the wallet:
///   parse offer → metadata → token → nonce → proof → credential → store.
/// Each HTTP hop is a separate call so the proxy logs every step.
/// </summary>
public sealed class WalletClient
{
    private readonly HttpClient _http;
    private readonly HolderKeyMaterial _keys;
    private readonly WalletStore _store;
    private readonly PopJwtService _pop;
    private readonly ILogger<WalletClient> _log;

    public WalletClient(
        HttpClient http,
        HolderKeyMaterial keys,
        WalletStore store,
        PopJwtService pop,
        ILogger<WalletClient> log)
    {
        _http = http;
        _keys = keys;
        _store = store;
        _pop = pop;
        _log = log;
    }

    public async Task<StoredCredential> RedeemOfferAsync(string offerUri, CancellationToken ct = default)
    {
        var offer = CredentialOfferUri.ParseByValue(offerUri)
            ?? throw new InvalidOperationException("Phase 2A only supports by-value credential_offer URIs.");

        var preAuth = offer.Grants.PreAuthorizedCode
            ?? throw new InvalidOperationException("Offer is missing pre-authorized_code grant.");

        var configId = offer.CredentialConfigurationIds.First();

        // 1. Issuer metadata
        var issuerMetadataUrl = $"{offer.CredentialIssuer.TrimEnd('/')}{Oid4VciConstants.IssuerMetadataPath}";
        var issuerMetadata = await _http.GetFromJsonAsync<IssuerMetadata>(issuerMetadataUrl, ct)
            ?? throw new InvalidOperationException("Issuer metadata returned null.");

        // 2. AS metadata (pick the first advertised; falls back to credential_issuer)
        var asBase = (issuerMetadata.AuthorizationServers?.FirstOrDefault() ?? offer.CredentialIssuer).TrimEnd('/');
        var asMetadataUrl = $"{asBase}{Oid4VciConstants.AuthorizationServerMetadataPath}";
        var asMetadata = await _http.GetFromJsonAsync<AuthorizationServerMetadata>(asMetadataUrl, ct)
            ?? throw new InvalidOperationException("AS metadata returned null.");

        // 3. Token exchange (pre-authorized_code, form-encoded)
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = Oid4VciConstants.PreAuthorizedCodeGrantType,
            ["pre-authorized_code"] = preAuth.PreAuthorizedCode
        });
        var tokenResp = await _http.PostAsync(asMetadata.TokenEndpoint, tokenRequest, ct);
        tokenResp.EnsureSuccessStatusCode();
        var token = await tokenResp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Token response returned null.");

        // 4. Fetch a nonce for the proof
        if (string.IsNullOrEmpty(issuerMetadata.NonceEndpoint))
            throw new InvalidOperationException("Issuer metadata is missing nonce_endpoint.");
        var nonceResp = await _http.PostAsync(issuerMetadata.NonceEndpoint, content: null, ct);
        nonceResp.EnsureSuccessStatusCode();
        var nonce = await nonceResp.Content.ReadFromJsonAsync<NonceResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Nonce response returned null.");

        // 5. Build proof JWT bound to the credential_issuer audience
        var proofJwt = _pop.BuildEd25519ProofJwt(
            _keys.KeyPair.PrivateKey,
            _keys.KeyPair.PublicKey,
            audience: offer.CredentialIssuer,
            nonce: nonce.CNonce);

        // 6. Credential request
        var credRequest = new CredentialRequest
        {
            CredentialConfigurationId = configId,
            Proofs = new CredentialRequestProofs { Jwt = [proofJwt] }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, issuerMetadata.CredentialEndpoint)
        {
            Content = JsonContent.Create(credRequest)
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
        var credResp = await _http.SendAsync(req, ct);
        credResp.EnsureSuccessStatusCode();
        var credResponse = await credResp.Content.ReadFromJsonAsync<CredentialResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Credential response returned null.");

        var jwt = credResponse.Credentials[0].Credential;
        var stored = _store.Store(jwt, offer.CredentialIssuer, configId);

        _log.LogInformation("Wallet stored credential {Id} from {Issuer}", stored.Id, offer.CredentialIssuer);
        return stored;
    }
}
