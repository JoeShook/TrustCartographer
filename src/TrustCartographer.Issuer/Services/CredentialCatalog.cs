using TrustCartographer.Core.Oid4Vci;

namespace TrustCartographer.Issuer.Services;

/// <summary>
/// The set of credentials this Issuer is willing to mint. Drives both the
/// <c>credential_configurations_supported</c> map advertised in metadata and
/// the issuance branch picked by the credential endpoint. Phase 2A ships one.
/// </summary>
public sealed class CredentialCatalog
{
    public const string PractitionerLicenseId = "PractitionerLicenseCredential";

    public IReadOnlyDictionary<string, CredentialConfiguration> SupportedConfigurations { get; } =
        new Dictionary<string, CredentialConfiguration>
        {
            [PractitionerLicenseId] = new CredentialConfiguration
            {
                Format = Oid4VciConstants.JwtVcJsonFormat,
                Scope = PractitionerLicenseId,
                CryptographicBindingMethodsSupported = ["did:key"],
                CredentialSigningAlgValuesSupported = ["EdDSA"],
                ProofTypesSupported = new Dictionary<string, ProofTypeMetadata>
                {
                    ["jwt"] = new ProofTypeMetadata
                    {
                        ProofSigningAlgValuesSupported = ["EdDSA"]
                    }
                },
                CredentialDefinition = new CredentialDefinition
                {
                    Type = ["VerifiableCredential", PractitionerLicenseId]
                },
                Display =
                [
                    new DisplayInfo
                    {
                        Name = "Practitioner License Credential",
                        Locale = "en-US",
                        Description = "Asserts an active medical license issued by the State Medical Board."
                    }
                ]
            }
        };

    public IReadOnlyDictionary<string, object?> DefaultSubjectClaimsFor(string id) =>
        id switch
        {
            PractitionerLicenseId => new Dictionary<string, object?>
            {
                ["practitionerName"] = "Dr. Jane Smith",
                ["licenseNumber"] = "MD-2026-04891",
                ["specialty"] = "Internal Medicine",
                ["jurisdiction"] = "WA"
            },
            _ => throw new KeyNotFoundException($"Unknown credential_configuration_id: {id}")
        };

    public string CredentialTypeFor(string id) =>
        id switch
        {
            PractitionerLicenseId => PractitionerLicenseId,
            _ => throw new KeyNotFoundException($"Unknown credential_configuration_id: {id}")
        };
}
