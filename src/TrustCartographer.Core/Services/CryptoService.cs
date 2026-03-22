using System.Text;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using TrustCartographer.Core.Models;

namespace TrustCartographer.Core.Services;

public class CryptoService
{
    private static readonly SecureRandom Random = new();

    public KeyPairInfo GenerateEd25519KeyPair()
    {
        var keyPairGenerator = new Ed25519KeyPairGenerator();
        keyPairGenerator.Init(new Ed25519KeyGenerationParameters(Random));
        var keyPair = keyPairGenerator.GenerateKeyPair();

        var privateKey = (Ed25519PrivateKeyParameters)keyPair.Private;
        var publicKey = (Ed25519PublicKeyParameters)keyPair.Public;

        return new KeyPairInfo
        {
            Algorithm = "Ed25519",
            PrivateKey = privateKey.GetEncoded(),
            PublicKey = publicKey.GetEncoded()
        };
    }

    public KeyPairInfo GenerateSecp256k1KeyPair()
    {
        var domainParams = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
        var ecParams = new ECDomainParameters(domainParams.Curve, domainParams.G, domainParams.N, domainParams.H);

        var keyGenParams = new ECKeyGenerationParameters(ecParams, Random);
        var generator = new ECKeyPairGenerator();
        generator.Init(keyGenParams);
        var keyPair = generator.GenerateKeyPair();

        var privateKey = (ECPrivateKeyParameters)keyPair.Private;
        var publicKey = (ECPublicKeyParameters)keyPair.Public;

        return new KeyPairInfo
        {
            Algorithm = "secp256k1",
            PrivateKey = privateKey.D.ToByteArrayUnsigned(),
            PublicKey = publicKey.Q.GetEncoded(true) // compressed
        };
    }

    public SignatureResult SignEd25519(byte[] privateKeyBytes, string message)
    {
        var privateKey = new Ed25519PrivateKeyParameters(privateKeyBytes, 0);
        var signer = new Ed25519Signer();
        signer.Init(true, privateKey);

        var messageBytes = Encoding.UTF8.GetBytes(message);
        signer.BlockUpdate(messageBytes, 0, messageBytes.Length);
        var signature = signer.GenerateSignature();

        return new SignatureResult { Signature = signature, IsValid = true };
    }

    public bool VerifyEd25519(byte[] publicKeyBytes, string message, byte[] signature)
    {
        var publicKey = new Ed25519PublicKeyParameters(publicKeyBytes, 0);
        var verifier = new Ed25519Signer();
        verifier.Init(false, publicKey);

        var messageBytes = Encoding.UTF8.GetBytes(message);
        verifier.BlockUpdate(messageBytes, 0, messageBytes.Length);

        return verifier.VerifySignature(signature);
    }

    public byte[] ComputeSha256(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        return System.Security.Cryptography.SHA256.HashData(inputBytes);
    }
}
