using System.Text.Json.Serialization;

namespace TrustCartographer.Core.Oid4Vp;

/// <summary>
/// Wire shape of the OID4VP Authorization Request the Verifier sends to the
/// Wallet (OpenID4VP 1.0, §5). Delivered by reference (<c>request_uri</c>) so the
/// wallet dereferences it — one extra captured hop in the Live Flow.
/// Simplified: a single required credential type instead of full DCQL.
/// </summary>
public record PresentationRequest
{
    [JsonPropertyName("response_type")]
    public string ResponseType { get; init; } = Oid4VpConstants.VpTokenResponseType;

    /// <summary>Verifier identity. Here it's the Verifier's public base URL.</summary>
    [JsonPropertyName("client_id")]
    public required string ClientId { get; init; }

    [JsonPropertyName("nonce")]
    public required string Nonce { get; init; }

    /// <summary>Where the wallet POSTs the vp_token (direct_post).</summary>
    [JsonPropertyName("response_uri")]
    public required string ResponseUri { get; init; }

    [JsonPropertyName("response_mode")]
    public string ResponseMode { get; init; } = Oid4VpConstants.ResponseModeDirectPost;

    /// <summary>Opaque request id; echoed back as <c>state</c> in the response.</summary>
    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("presentation_definition")]
    public required PresentationDefinition PresentationDefinition { get; init; }
}

/// <summary>
/// Simplified presentation_definition: the Verifier asks for one credential type.
/// </summary>
public record PresentationDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("required_credential_type")]
    public required string RequiredCredentialType { get; init; }

    [JsonPropertyName("purpose")]
    public string? Purpose { get; init; }
}
