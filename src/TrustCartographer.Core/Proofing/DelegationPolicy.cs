namespace TrustCartographer.Core.Proofing;

/// <summary>
/// The delegated identity-proofing rules from NIST 800-63A rev4 (as applied by the
/// Portable Identity Assurance proposal): a verified responsible adult vouches for a
/// minor. Two structural principles:
///  • <b>Ceiling</b> — the minor's effective assurance can never exceed the delegate's
///    (higher assurance can't be derived from a weaker proofing event).
///  • <b>Discount</b> — delegated proofing is a recognised-but-distinct pathway, so the
///    result is marked "via delegation" rather than treated as direct proofing.
/// Below the minimum delegate floor, no minor credential may be issued.
///
/// Pure policy — an SDK seed; no UI/transport.
/// </summary>
public sealed class DelegationPolicy
{
    /// <summary>Minimum delegate assurance below which issuance is not permitted.</summary>
    public const IdialLevel MinimumDelegateFloor = IdialLevel.Idial2;

    public DelegationResult Evaluate(IdialLevel delegateIdial)
    {
        if (delegateIdial < MinimumDelegateFloor)
            return new DelegationResult(
                delegateIdial, IdialLevel.Unproofed, Issuable: false,
                $"Delegate assurance {delegateIdial} is below the minimum floor ({MinimumDelegateFloor}); " +
                "a minor credential cannot be issued on this basis.");

        // Ceiling applies (effective ≤ delegate); the delegation discount is represented
        // by the explicit "via delegated proofing" qualifier rather than a numeric drop,
        // matching the proposal's delegate→minor mapping table.
        return new DelegationResult(
            delegateIdial, delegateIdial, Issuable: true,
            $"Minor effective assurance {delegateIdial} via delegated proofing " +
            "(ceiling = delegate level; delegation discount noted).");
    }
}

public record DelegationResult(
    IdialLevel DelegateIdial,
    IdialLevel MinorEffectiveIdial,
    bool Issuable,
    string Reason);
