using System.Collections.Concurrent;
using System.Security.Cryptography;
using TrustCartographer.Core.Oid4Vci;

namespace TrustCartographer.Issuer.Services;

/// <summary>
/// In-memory state for Phase 2A: pending credential offers keyed by their
/// pre-authorized code, issued access tokens, and one-shot c_nonces. All three
/// live for the process lifetime; the visualisation only needs minutes, and
/// persistence would obscure the protocol mechanics this is meant to teach.
/// </summary>
public sealed class IssuerStores
{
    private readonly ConcurrentDictionary<string, OfferRecord> _offersByPreAuthCode = new();
    private readonly ConcurrentDictionary<string, AccessTokenRecord> _accessTokens = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _activeNonces = new();

    public OfferRecord CreateOffer(string credentialConfigurationId, IReadOnlyDictionary<string, object?> subjectClaims)
    {
        var record = new OfferRecord(
            OfferId: NewToken(8),
            PreAuthorizedCode: NewToken(24),
            CredentialConfigurationId: credentialConfigurationId,
            SubjectClaims: subjectClaims,
            CreatedAt: DateTimeOffset.UtcNow);
        _offersByPreAuthCode[record.PreAuthorizedCode] = record;
        return record;
    }

    public OfferRecord? RedeemOffer(string preAuthCode) =>
        _offersByPreAuthCode.TryRemove(preAuthCode, out var record) ? record : null;

    public AccessTokenRecord IssueAccessToken(OfferRecord offer, TimeSpan ttl)
    {
        var token = NewToken(32);
        var record = new AccessTokenRecord(
            AccessToken: token,
            CredentialConfigurationId: offer.CredentialConfigurationId,
            SubjectClaims: offer.SubjectClaims,
            ExpiresAt: DateTimeOffset.UtcNow + ttl);
        _accessTokens[token] = record;
        return record;
    }

    public AccessTokenRecord? GetAccessToken(string token) =>
        _accessTokens.TryGetValue(token, out var record) && record.ExpiresAt > DateTimeOffset.UtcNow
            ? record : null;

    public string MintNonce(TimeSpan ttl)
    {
        var nonce = NewToken(16);
        _activeNonces[nonce] = DateTimeOffset.UtcNow + ttl;
        return nonce;
    }

    /// <summary>Consume a nonce — single-use. Returns true if it was valid and has now been burned.</summary>
    public bool ConsumeNonce(string nonce)
    {
        if (!_activeNonces.TryRemove(nonce, out var expiresAt)) return false;
        return expiresAt > DateTimeOffset.UtcNow;
    }

    private static string NewToken(int byteLen)
    {
        Span<byte> buf = stackalloc byte[byteLen];
        RandomNumberGenerator.Fill(buf);
        return Convert.ToBase64String(buf).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

public record OfferRecord(
    string OfferId,
    string PreAuthorizedCode,
    string CredentialConfigurationId,
    IReadOnlyDictionary<string, object?> SubjectClaims,
    DateTimeOffset CreatedAt);

public record AccessTokenRecord(
    string AccessToken,
    string CredentialConfigurationId,
    IReadOnlyDictionary<string, object?> SubjectClaims,
    DateTimeOffset ExpiresAt);
