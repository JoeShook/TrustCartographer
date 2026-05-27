using System.Text.Json.Serialization;

namespace TrustCartographer.Core.Oid4Vci;

/// <summary>
/// Wire shape of the Credential Offer sent from Issuer → Holder (Section 4 of OID4VCI 1.0).
/// May be delivered by value (in a query string) or by reference (credential_offer_uri).
/// </summary>
public record CredentialOffer
{
    [JsonPropertyName("credential_issuer")]
    public required string CredentialIssuer { get; init; }

    [JsonPropertyName("credential_configuration_ids")]
    public required IReadOnlyList<string> CredentialConfigurationIds { get; init; }

    [JsonPropertyName("grants")]
    public required CredentialOfferGrants Grants { get; init; }
}

public record CredentialOfferGrants
{
    [JsonPropertyName(Oid4VciConstants.PreAuthorizedCodeGrantType)]
    public PreAuthorizedCodeGrant? PreAuthorizedCode { get; init; }
}

public record PreAuthorizedCodeGrant
{
    [JsonPropertyName("pre-authorized_code")]
    public required string PreAuthorizedCode { get; init; }

    [JsonPropertyName("tx_code")]
    public TxCodeRequirement? TxCode { get; init; }

    [JsonPropertyName("authorization_server")]
    public string? AuthorizationServer { get; init; }
}

public record TxCodeRequirement
{
    [JsonPropertyName("input_mode")]
    public string? InputMode { get; init; }  // "numeric" | "text"

    [JsonPropertyName("length")]
    public int? Length { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
