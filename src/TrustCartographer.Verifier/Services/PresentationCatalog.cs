namespace TrustCartographer.Verifier.Services;

/// <summary>
/// What this Verifier asks for. The Hospital EHR only needs to confirm an active
/// practitioner license before granting access. Mirrors the Issuer's
/// CredentialCatalog: the single source of truth for the presentation_definition.
/// </summary>
public sealed class PresentationCatalog
{
    public const string RequiredCredentialType = "PractitionerLicenseCredential";

    public string DisplayName => "Hospital EHR";

    public string Purpose => "Confirm the practitioner holds an active medical license before granting chart access.";
}
