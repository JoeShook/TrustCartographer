namespace TrustCartographer.Issuing;

/// <summary>
/// Seam by which an issuer decides the <b>identity-assurance basis</b> for a credential it is
/// about to offer, and contributes assurance claims that travel inside the credential.
/// Kept transport-free in the kit (an SDK seam): static issuers supply a fixed pathway level;
/// the IAL2-CSP issuer implements one that consults the Identity-CSP's proofing event.
/// If no <see cref="IAssuranceSource"/> is registered, issuance proceeds with no assurance
/// enrichment (the original trust-triangle behaviour).
/// </summary>
public interface IAssuranceSource
{
    Task<AssuranceResolution> ResolveAsync(AssuranceResolutionContext context, CancellationToken ct = default);
}

/// <summary>
/// Inputs an <see cref="IAssuranceSource"/> needs at offer time. <see cref="ProxyBaseUrl"/> is the
/// public origin the offer arrived through (scheme://host, no path prefix) so a source can reach
/// peer services through the same proxy that captures Live Flow traffic.
/// </summary>
public sealed record AssuranceResolutionContext(
    string CredentialConfigurationId,
    string? ProofingEventId,
    string ProxyBaseUrl);

/// <summary>
/// The outcome of resolving assurance. <see cref="Allowed"/> is the gate: a source can refuse
/// issuance entirely (e.g. proofing below the delegate floor). <see cref="Claims"/> are merged
/// into the credential's subject claims (selectively disclosable for SD-JWT).
/// </summary>
public sealed record AssuranceResolution(
    bool Allowed,
    IReadOnlyDictionary<string, object?>? Claims = null,
    string? RefusalReason = null)
{
    public static AssuranceResolution Refuse(string reason) => new(false, null, reason);
    public static AssuranceResolution Allow(IReadOnlyDictionary<string, object?> claims) => new(true, claims, null);
}

/// <summary>
/// A fixed-pathway assurance source: every offer carries the same representative assurance level
/// for this issuer's pathway. Models payer-enrollment / provider-in-person issuers whose pathway
/// implies a known IDIAL without consulting a separate CSP.
/// </summary>
public sealed class StaticAssuranceSource : IAssuranceSource
{
    private readonly IReadOnlyDictionary<string, object?> _claims;

    public StaticAssuranceSource(string assuranceLevel, string pathway, string method, string basis)
    {
        _claims = new Dictionary<string, object?>
        {
            ["assurance_level"] = assuranceLevel,
            ["assurance_pathway"] = pathway,
            ["assurance_method"] = method,
            ["assurance_basis"] = basis
        };
    }

    public Task<AssuranceResolution> ResolveAsync(AssuranceResolutionContext context, CancellationToken ct = default) =>
        Task.FromResult(AssuranceResolution.Allow(_claims));
}
