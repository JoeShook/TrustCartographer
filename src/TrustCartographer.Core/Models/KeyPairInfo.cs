namespace TrustCartographer.Core.Models;

public class KeyPairInfo
{
    public string Algorithm { get; set; } = string.Empty;
    public byte[] PrivateKey { get; set; } = [];
    public byte[] PublicKey { get; set; } = [];
    public string PrivateKeyHex => Convert.ToHexString(PrivateKey).ToLowerInvariant();
    public string PublicKeyHex => Convert.ToHexString(PublicKey).ToLowerInvariant();
    public string PrivateKeyBase64Url => Base64UrlEncode(PrivateKey);
    public string PublicKeyBase64Url => Base64UrlEncode(PublicKey);

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
