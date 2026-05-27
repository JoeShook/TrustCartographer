using System.Text.Json.Serialization;

namespace TrustCartographer.Core.Oid4Vci;

/// <summary>
/// Minimal JWK (RFC 7517) shape sufficient for embedding an Ed25519 public key
/// inside an OID4VCI proof JWT header (Section 8.2 of OID4VCI 1.0 → RFC 8037).
/// </summary>
public record Jwk
{
    [JsonPropertyName("kty")]
    public required string Kty { get; init; }

    [JsonPropertyName("crv")]
    public string? Crv { get; init; }

    [JsonPropertyName("x")]
    public string? X { get; init; }

    public static Jwk FromEd25519PublicKey(byte[] publicKey) => new()
    {
        Kty = "OKP",
        Crv = "Ed25519",
        X = Base64Url.Encode(publicKey)
    };
}
