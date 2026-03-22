namespace TrustCartographer.Core.Models;

public class SignatureResult
{
    public byte[] Signature { get; set; } = [];
    public string SignatureHex => Convert.ToHexString(Signature).ToLowerInvariant();
    public string SignatureBase64Url => Convert.ToBase64String(Signature)
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public bool IsValid { get; set; }
}
