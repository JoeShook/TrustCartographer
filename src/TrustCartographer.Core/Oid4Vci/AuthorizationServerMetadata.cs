using System.Text.Json.Serialization;

namespace TrustCartographer.Core.Oid4Vci;

/// <summary>
/// Authorization server metadata (RFC 8414) carrying the OID4VCI-specific
/// pre-authorized grant flag from Section 11.2.3. The Issuer acts as its
/// own AS in our setup, so this lives next to <see cref="IssuerMetadata"/>.
/// </summary>
public record AuthorizationServerMetadata
{
    [JsonPropertyName("issuer")]
    public required string Issuer { get; init; }

    [JsonPropertyName("token_endpoint")]
    public required string TokenEndpoint { get; init; }

    [JsonPropertyName("grant_types_supported")]
    public required IReadOnlyList<string> GrantTypesSupported { get; init; }

    [JsonPropertyName("pre-authorized_grant_anonymous_access_supported")]
    public bool? PreAuthorizedGrantAnonymousAccessSupported { get; init; }
}
