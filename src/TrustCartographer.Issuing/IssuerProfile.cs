namespace TrustCartographer.Issuing;

/// <summary>
/// The issuing organisation's public identity and the assurance pathway it operates.
/// Injected per host so a single issuance kit can back many distinct issuer apps
/// (payer / provider / IAL2-CSP) without each re-implementing the OID4VCI mechanics.
/// The <see cref="Pathway"/>/<see cref="PathwayLabel"/> describe the Portable Identity
/// Assurance issuance pathway (A enrollment, B in-person, C IAL2 CSP) this issuer maps to.
/// </summary>
public sealed class IssuerProfile
{
    /// <summary>Human-facing issuer name surfaced in OID4VCI metadata <c>display</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Short pathway code, e.g. <c>A-enrollment</c>, <c>B-in-person</c>, <c>C-ial2-csp</c>.</summary>
    public required string Pathway { get; init; }

    /// <summary>Human-readable pathway description for UIs.</summary>
    public required string PathwayLabel { get; init; }
}
