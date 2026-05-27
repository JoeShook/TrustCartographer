using System.Text.Json.Serialization;

namespace TrustCartographer.Core.Oid4Vci;

/// <summary>
/// Token endpoint payloads. Request is sent application/x-www-form-urlencoded
/// (Section 6 of OID4VCI 1.0); response is JSON.
/// </summary>
public record TokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; init; }

    [JsonPropertyName("authorization_details")]
    public IReadOnlyList<AuthorizationDetail>? AuthorizationDetails { get; init; }
}

/// <summary>
/// RFC 9396 Rich Authorization Request entry. OID4VCI uses
/// <c>type = "openid_credential"</c> to attach an issued
/// <c>credential_configuration_id</c> to the access token.
/// </summary>
public record AuthorizationDetail
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("credential_configuration_id")]
    public string? CredentialConfigurationId { get; init; }

    [JsonPropertyName("credential_identifiers")]
    public IReadOnlyList<string>? CredentialIdentifiers { get; init; }
}

public record NonceResponse
{
    [JsonPropertyName("c_nonce")]
    public required string CNonce { get; init; }
}
