using System.Text.Json.Serialization;

namespace TrustCartographer.Core.Oid4Vci;

/// <summary>
/// Issuer metadata returned from <c>/.well-known/openid-credential-issuer</c>
/// (Section 11 of OID4VCI 1.0). Only the fields we actually populate are surfaced here —
/// adding more is additive.
/// </summary>
public record IssuerMetadata
{
    [JsonPropertyName("credential_issuer")]
    public required string CredentialIssuer { get; init; }

    [JsonPropertyName("authorization_servers")]
    public IReadOnlyList<string>? AuthorizationServers { get; init; }

    [JsonPropertyName("credential_endpoint")]
    public required string CredentialEndpoint { get; init; }

    [JsonPropertyName("nonce_endpoint")]
    public string? NonceEndpoint { get; init; }

    [JsonPropertyName("credential_configurations_supported")]
    public required IReadOnlyDictionary<string, CredentialConfiguration> CredentialConfigurationsSupported { get; init; }

    [JsonPropertyName("display")]
    public IReadOnlyList<DisplayInfo>? Display { get; init; }
}

public record CredentialConfiguration
{
    [JsonPropertyName("format")]
    public required string Format { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("cryptographic_binding_methods_supported")]
    public IReadOnlyList<string>? CryptographicBindingMethodsSupported { get; init; }

    [JsonPropertyName("credential_signing_alg_values_supported")]
    public IReadOnlyList<string>? CredentialSigningAlgValuesSupported { get; init; }

    [JsonPropertyName("proof_types_supported")]
    public IReadOnlyDictionary<string, ProofTypeMetadata>? ProofTypesSupported { get; init; }

    [JsonPropertyName("credential_definition")]
    public CredentialDefinition? CredentialDefinition { get; init; }

    [JsonPropertyName("display")]
    public IReadOnlyList<DisplayInfo>? Display { get; init; }
}

public record ProofTypeMetadata
{
    [JsonPropertyName("proof_signing_alg_values_supported")]
    public required IReadOnlyList<string> ProofSigningAlgValuesSupported { get; init; }
}

public record CredentialDefinition
{
    [JsonPropertyName("type")]
    public required IReadOnlyList<string> Type { get; init; }
}

public record DisplayInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("locale")]
    public string? Locale { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
