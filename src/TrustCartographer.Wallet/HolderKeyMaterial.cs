using TrustCartographer.Core.Models;
using TrustCartographer.Core.Services;

namespace TrustCartographer.Wallet;

/// <summary>
/// The wallet's long-lived Ed25519 key + <c>did:key</c>. Phase 2A uses one key
/// per wallet (not per credential), trading unlinkability for clarity.
/// </summary>
public sealed class HolderKeyMaterial
{
    public KeyPairInfo KeyPair { get; }
    public string Did { get; }

    public HolderKeyMaterial(CryptoService crypto, DidKeyService didKey)
    {
        KeyPair = crypto.GenerateEd25519KeyPair();
        Did = didKey.EncodeEd25519(KeyPair.PublicKey);
    }
}
