namespace TrustCartographer.Core.Oid4Vp;

/// <summary>
/// Well-known identifiers from OpenID for Verifiable Presentations 1.0.
/// Centralised so endpoint and parameter strings appear once. This is the
/// presentation/verification half of the trust triangle, mirroring
/// <see cref="Oid4Vci.Oid4VciConstants"/> for issuance.
/// </summary>
public static class Oid4VpConstants
{
    // Authorization request delivered to the wallet (deep link / QR).
    public const string PresentationRequestUriScheme = "openid4vp://";

    // response_type / response_mode values used by the request.
    public const string VpTokenResponseType = "vp_token";
    public const string ResponseModeDirectPost = "direct_post";

    // Media type of the holder-signed Verifiable Presentation JWT.
    public const string VpJwtType = "vp+jwt";

    // W3C VP @context + type token.
    public const string VpContext = "https://www.w3.org/ns/credentials/v2";
    public const string VerifiablePresentationType = "VerifiablePresentation";
}
