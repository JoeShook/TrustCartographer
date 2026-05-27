using System.Text.Json;
using System.Text.Json.Nodes;
using TrustCartographer.Core.Services;

namespace TrustCartographer.Core.Oid4Vci;

/// <summary>
/// Builds and verifies OpenID4VCI proof-of-possession JWTs (Section 8.2.1.1 of
/// OID4VCI 1.0). These prove the Holder controls the key that will be bound
/// into the issued credential.
/// </summary>
public class PopJwtService
{
    private readonly CryptoService _crypto;
    private readonly DidKeyService _didKey;

    public PopJwtService(CryptoService crypto, DidKeyService didKey)
    {
        _crypto = crypto;
        _didKey = didKey;
    }

    /// <summary>
    /// Build a proof JWT signed with the Holder's Ed25519 key. Embeds the
    /// public key as a <c>jwk</c> in the header (the simplest binding option
    /// — no DID resolution needed by the Issuer).
    /// </summary>
    public string BuildEd25519ProofJwt(
        byte[] holderPrivateKey,
        byte[] holderPublicKey,
        string audience,
        string nonce,
        DateTimeOffset? issuedAt = null)
    {
        var iat = (issuedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();

        var header = new JsonObject
        {
            ["alg"] = "EdDSA",
            ["typ"] = Oid4VciConstants.ProofJwtType,
            ["jwk"] = JsonSerializer.SerializeToNode(Jwk.FromEd25519PublicKey(holderPublicKey))
        };

        var payload = new JsonObject
        {
            ["aud"] = audience,
            ["iat"] = iat,
            ["nonce"] = nonce
        };

        var headerSeg = Base64Url.Encode(header.ToJsonString());
        var payloadSeg = Base64Url.Encode(payload.ToJsonString());
        var signingInput = $"{headerSeg}.{payloadSeg}";

        var sig = _crypto.SignEd25519(holderPrivateKey, signingInput);
        var sigSeg = Base64Url.Encode(sig.Signature!);

        return $"{signingInput}.{sigSeg}";
    }

    /// <summary>
    /// Verify a proof JWT. Pulls the Holder public key from the header (jwk
    /// binding) and checks signature, audience, and nonce. Returns the decoded
    /// header/payload alongside the verdict so callers can render them.
    /// </summary>
    public PopJwtVerification Verify(string jwt, string expectedAudience, string expectedNonce)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return PopJwtVerification.Fail("Proof JWT must have 3 segments.");

        JsonNode? header;
        JsonNode? payload;
        try
        {
            header = JsonNode.Parse(Base64Url.DecodeToString(parts[0]));
            payload = JsonNode.Parse(Base64Url.DecodeToString(parts[1]));
        }
        catch (Exception ex)
        {
            return PopJwtVerification.Fail($"Could not parse segments: {ex.Message}");
        }

        if (header is null || payload is null)
            return PopJwtVerification.Fail("Header or payload was null.");

        if ((string?)header["typ"] != Oid4VciConstants.ProofJwtType)
            return PopJwtVerification.Fail($"typ must be {Oid4VciConstants.ProofJwtType}.");
        if ((string?)header["alg"] != "EdDSA")
            return PopJwtVerification.Fail("Only alg=EdDSA is supported in Phase 2A.");

        var jwkNode = header["jwk"];
        if (jwkNode is null)
            return PopJwtVerification.Fail("Phase 2A requires jwk binding in the proof header.");

        var jwk = JsonSerializer.Deserialize<Jwk>(jwkNode.ToJsonString());
        if (jwk is null || jwk.Crv != "Ed25519" || string.IsNullOrEmpty(jwk.X))
            return PopJwtVerification.Fail("Header jwk must be an Ed25519 OKP key with x.");

        var holderPublicKey = Base64Url.Decode(jwk.X!);
        if (holderPublicKey.Length != 32)
            return PopJwtVerification.Fail("Decoded x is not 32 bytes.");

        var signingInput = $"{parts[0]}.{parts[1]}";
        var signature = Base64Url.Decode(parts[2]);
        if (!_crypto.VerifyEd25519(holderPublicKey, signingInput, signature))
            return PopJwtVerification.Fail("Signature did not verify.", header, payload);

        if ((string?)payload["aud"] != expectedAudience)
            return PopJwtVerification.Fail($"aud mismatch (expected {expectedAudience}).", header, payload);
        if ((string?)payload["nonce"] != expectedNonce)
            return PopJwtVerification.Fail("nonce mismatch (replay or stale proof).", header, payload);

        var holderDid = _didKey.EncodeEd25519(holderPublicKey);

        return new PopJwtVerification(
            Valid: true,
            Message: "OK",
            HeaderJson: header.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
            PayloadJson: payload.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
            HolderPublicKey: holderPublicKey,
            HolderDidKey: holderDid);
    }
}

public record PopJwtVerification(
    bool Valid,
    string Message,
    string? HeaderJson,
    string? PayloadJson,
    byte[]? HolderPublicKey,
    string? HolderDidKey)
{
    public static PopJwtVerification Fail(string msg, JsonNode? header = null, JsonNode? payload = null) =>
        new(false, msg,
            header?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
            payload?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
            null, null);
}
