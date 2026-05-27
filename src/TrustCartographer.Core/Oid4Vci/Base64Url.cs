using System.Text;

namespace TrustCartographer.Core.Oid4Vci;

/// <summary>
/// Tiny base64url codec — same convention used by JWT/JWS (RFC 7515 Appendix C).
/// Lives in <c>Core.Oid4Vci</c> because the OID4VCI flow speaks JWT at every hop;
/// <see cref="Services.VcService"/> has the same logic inlined as a private helper
/// (kept there for the standalone walkthrough story).
/// </summary>
public static class Base64Url
{
    public static string Encode(string utf8) => Encode(Encoding.UTF8.GetBytes(utf8));

    public static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] Decode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    public static string DecodeToString(string s) => Encoding.UTF8.GetString(Decode(s));
}
