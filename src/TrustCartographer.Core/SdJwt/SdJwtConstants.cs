namespace TrustCartographer.Core.SdJwt;

/// <summary>
/// Identifiers from the IETF SD-JWT and SD-JWT VC specs (selective-disclosure JWTs).
/// SD-JWT lets an issuer commit to a set of claims while letting the holder reveal
/// any subset to a verifier — the credential-format foundation of the Portable
/// Identity Assurance use cases.
/// </summary>
public static class SdJwtConstants
{
    /// <summary>Media type / <c>typ</c> for an SD-JWT Verifiable Credential.</summary>
    public const string SdJwtVcType = "vc+sd-jwt";

    /// <summary>Media type / <c>typ</c> for the holder's Key Binding JWT.</summary>
    public const string KbJwtType = "kb+jwt";

    /// <summary>Hash algorithm name placed in <c>_sd_alg</c>.</summary>
    public const string SdAlg = "sha-256";

    /// <summary>The <c>~</c> separator between the issuer JWT, disclosures, and KB-JWT.</summary>
    public const char Delimiter = '~';
}
