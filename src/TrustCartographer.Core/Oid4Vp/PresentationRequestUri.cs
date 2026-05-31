using System.Text.Json;
using System.Web;

namespace TrustCartographer.Core.Oid4Vp;

/// <summary>
/// Encodes/decodes the <c>openid4vp://?request_uri=...</c> (by-reference) and
/// <c>?request=...</c> (by-value) authorization-request URIs the Verifier hands
/// to the Wallet (OpenID4VP 1.0, §5). Mirrors
/// <see cref="Oid4Vci.CredentialOfferUri"/>. By-reference is what the demo uses
/// so the wallet dereferences the request (an extra visible hop).
/// </summary>
public static class PresentationRequestUri
{
    public static string BuildByReference(string requestUri) =>
        $"{Oid4VpConstants.PresentationRequestUriScheme}?request_uri={HttpUtility.UrlEncode(requestUri)}";

    public static string BuildByValue(PresentationRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        return $"{Oid4VpConstants.PresentationRequestUriScheme}?request={HttpUtility.UrlEncode(json)}";
    }

    /// <summary>Returns the dereferenceable request_uri, or null if this is a by-value URI.</summary>
    public static string? ParseByReference(string uri)
    {
        var (key, value) = ParseQuery(uri);
        return key == "request_uri" ? value : null;
    }

    /// <summary>Returns the inline request object, or null if this carries a request_uri instead.</summary>
    public static PresentationRequest? ParseByValue(string uri)
    {
        var (key, value) = ParseQuery(uri);
        if (key != "request") return null;
        return JsonSerializer.Deserialize<PresentationRequest>(value);
    }

    private static (string Key, string Value) ParseQuery(string uri)
    {
        var qIdx = uri.IndexOf('?');
        if (qIdx < 0) return (string.Empty, string.Empty);
        var query = uri[(qIdx + 1)..];
        var parsed = HttpUtility.ParseQueryString(query);
        var key = parsed.AllKeys.FirstOrDefault() ?? string.Empty;
        return (key, parsed[key] ?? string.Empty);
    }
}
