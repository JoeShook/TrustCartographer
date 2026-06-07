namespace TrustCartographer.Core.Proofing;

/// <summary>
/// A recorded delegated identity-proofing event (NIST 800-63A rev4): a verified
/// responsible adult (the delegate) vouches for a minor; the CSP acts as
/// CSP-of-record. Issuers consult this when minting the minor's credential so the
/// assurance basis travels in the credential payload rather than being inferred.
/// </summary>
public record ProofingEvent(
    string Id,
    string DelegateId,
    string Method,
    IdialLevel DelegateIdial,
    IdialLevel MinorEffectiveIdial,
    bool Issuable,
    string Reason,
    DateTimeOffset At);
