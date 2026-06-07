using TrustCartographer.Core.Models;
using TrustCartographer.Core.Services;

namespace TrustCartographer.Issuing;

/// <summary>
/// Holds the Issuer's Ed25519 key pair and the resulting <c>did:key</c> for the
/// lifetime of the process. Generated once at startup so visualisations across
/// the run-up to issuance see a stable issuer identity.
/// </summary>
public sealed class IssuerKeyMaterial
{
    public KeyPairInfo KeyPair { get; }
    public string Did { get; }
    public string VerificationMethodId { get; }

    public IssuerKeyMaterial(CryptoService crypto, DidKeyService didKey)
    {
        KeyPair = crypto.GenerateEd25519KeyPair();
        Did = didKey.EncodeEd25519(KeyPair.PublicKey);
        VerificationMethodId = didKey.VerificationMethodId(Did);
    }
}
