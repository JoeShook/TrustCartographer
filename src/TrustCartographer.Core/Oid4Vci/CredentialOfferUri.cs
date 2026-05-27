using System.Text.Json;
using System.Web;

namespace TrustCartographer.Core.Oid4Vci;

/// <summary>
/// Encodes/decodes the <c>openid-credential-offer://?credential_offer=...</c>
/// (by-value) and <c>?credential_offer_uri=...</c> (by-reference) URIs that
/// Issuers hand to Wallets (Section 4.1 of OID4VCI 1.0). By-value is what the
/// demo uses — by-reference is here for symmetry when we add QR/deep-link work.
/// </summary>
public static class CredentialOfferUri
{
    public static string BuildByValue(CredentialOffer offer)
    {
        var json = JsonSerializer.Serialize(offer);
        return $"{Oid4VciConstants.CredentialOfferUriScheme}?credential_offer={HttpUtility.UrlEncode(json)}";
    }

    public static string BuildByReference(string offerUri) =>
        $"{Oid4VciConstants.CredentialOfferUriScheme}?credential_offer_uri={HttpUtility.UrlEncode(offerUri)}";

    /// <summary>Parses by-value offers. Returns null if the URI carries an offer_uri instead.</summary>
    public static CredentialOffer? ParseByValue(string uri)
    {
        var (key, value) = ParseQuery(uri);
        if (key != "credential_offer") return null;
        return JsonSerializer.Deserialize<CredentialOffer>(value);
    }

    public static string? ParseByReference(string uri)
    {
        var (key, value) = ParseQuery(uri);
        return key == "credential_offer_uri" ? value : null;
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
