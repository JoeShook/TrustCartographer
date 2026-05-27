namespace TrustCartographer.Core.Oid4Vci;

/// <summary>
/// Well-known identifiers from OpenID for Verifiable Credential Issuance 1.0
/// (draft 17). Centralised so endpoint and grant-type strings appear once.
/// </summary>
public static class Oid4VciConstants
{
    public const string PreAuthorizedCodeGrantType =
        "urn:ietf:params:oauth:grant-type:pre-authorized_code";

    public const string IssuerMetadataPath = "/.well-known/openid-credential-issuer";
    public const string AuthorizationServerMetadataPath = "/.well-known/oauth-authorization-server";

    public const string CredentialOfferUriScheme = "openid-credential-offer://";

    // Media types the credential endpoint and JWT proof use.
    public const string ProofJwtType = "openid4vci-proof+jwt";
    public const string VcJwtType = "vc+jwt";

    // Credential formats we currently support. Format strings are normative.
    public const string JwtVcJsonFormat = "jwt_vc_json";
}
