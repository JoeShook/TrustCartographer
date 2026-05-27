using System.Text.Json.Serialization;

namespace TrustCartographer.Core.Oid4Vci;

/// <summary>
/// Credential endpoint payloads (Section 8 of OID4VCI 1.0).
/// One request can carry multiple proofs (batch issuance); we accept that
/// shape even though Phase 2A only mints one VC per call.
/// </summary>
public record CredentialRequest
{
    [JsonPropertyName("credential_configuration_id")]
    public string? CredentialConfigurationId { get; init; }

    [JsonPropertyName("credential_identifier")]
    public string? CredentialIdentifier { get; init; }

    [JsonPropertyName("proofs")]
    public CredentialRequestProofs? Proofs { get; init; }
}

public record CredentialRequestProofs
{
    [JsonPropertyName("jwt")]
    public IReadOnlyList<string>? Jwt { get; init; }
}

public record CredentialResponse
{
    [JsonPropertyName("credentials")]
    public required IReadOnlyList<IssuedCredential> Credentials { get; init; }

    [JsonPropertyName("notification_id")]
    public string? NotificationId { get; init; }
}

public record IssuedCredential
{
    [JsonPropertyName("credential")]
    public required string Credential { get; init; }
}

/// <summary>
/// Standard OAuth 2.0 error response shape, reused for token + credential errors.
/// Specific error codes are listed in OID4VCI 1.0 Sections 6.3 and 8.3.
/// </summary>
public record OAuthErrorResponse
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}
