using SimpleBase;

namespace TrustCartographer.Core.Services;

/// <summary>
/// Minimal did:key implementation, intentionally written for visibility.
/// The DID *is* the public key: multicodec prefix + raw public key bytes,
/// multibase-encoded with base58btc ('z' prefix).
/// See specs/did-methods/did-key/ for the normative spec.
/// </summary>
public class DidKeyService
{
    // Multicodec prefix for ed25519-pub (0xED) as unsigned varint => [0xED, 0x01].
    public static readonly byte[] Ed25519MulticodecPrefix = [0xED, 0x01];

    /// <summary>
    /// Encodes an Ed25519 32-byte public key into a did:key identifier.
    /// </summary>
    public string EncodeEd25519(byte[] publicKey)
    {
        if (publicKey.Length != 32)
            throw new ArgumentException("Ed25519 public key must be 32 bytes.", nameof(publicKey));

        var prefixed = new byte[Ed25519MulticodecPrefix.Length + publicKey.Length];
        Buffer.BlockCopy(Ed25519MulticodecPrefix, 0, prefixed, 0, Ed25519MulticodecPrefix.Length);
        Buffer.BlockCopy(publicKey, 0, prefixed, Ed25519MulticodecPrefix.Length, publicKey.Length);

        var multibase = "z" + Base58.Bitcoin.Encode(prefixed);
        return $"did:key:{multibase}";
    }

    /// <summary>
    /// Decodes a did:key identifier back to raw public key bytes.
    /// Throws for non-Ed25519 or malformed inputs.
    /// </summary>
    public byte[] DecodeEd25519(string did)
    {
        const string prefix = "did:key:z";
        if (!did.StartsWith(prefix, StringComparison.Ordinal))
            throw new FormatException($"Expected did:key:z... identifier, got: {did}");

        var multibaseTail = did[prefix.Length..];
        var decoded = Base58.Bitcoin.Decode(multibaseTail).ToArray();

        if (decoded.Length != 34 || decoded[0] != 0xED || decoded[1] != 0x01)
            throw new FormatException("Not an Ed25519 did:key (wrong multicodec prefix).");

        return decoded[2..];
    }

    /// <summary>
    /// The verification method id used inside a did:key DID Document:
    ///   did:key:z6Mk...#z6Mk...
    /// </summary>
    public string VerificationMethodId(string did)
    {
        var fragment = did["did:key:".Length..];
        return $"{did}#{fragment}";
    }

    /// <summary>
    /// Builds a DID Document JSON for a did:key. Pure synthesis from the DID + key —
    /// no storage, no network. This is what makes did:key Phase-A friendly.
    /// </summary>
    public string SynthesizeDocument(string did, IReadOnlyCollection<string> purposes)
    {
        var multibase = did["did:key:".Length..];
        var vmId = VerificationMethodId(did);

        var purposeBlock = string.Join(",\n", purposes.Select(p => $"    \"{p}\": [\"{vmId}\"]"));

        return $$"""
            {
              "@context": [
                "https://www.w3.org/ns/did/v1",
                "https://w3id.org/security/suites/ed25519-2020/v1"
              ],
              "id": "{{did}}",
              "verificationMethod": [
                {
                  "id": "{{vmId}}",
                  "type": "Ed25519VerificationKey2020",
                  "controller": "{{did}}",
                  "publicKeyMultibase": "{{multibase}}"
                }
              ],
            {{purposeBlock}}
            }
            """;
    }
}
