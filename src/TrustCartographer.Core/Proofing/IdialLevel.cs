namespace TrustCartographer.Core.Proofing;

/// <summary>
/// FAST Identity Assurance Levels (HL7 FHIR Identity Matching IG) — a graduated
/// scale calibrated to healthcare evidence, more expressive than NIST 800-63A's
/// binary IAL1/IAL2. Rough mapping: IDIAL 4 ≈ NIST IAL2; IDIAL 2 ≈ IAL1; IDIAL 3
/// is the practically-important middle ground most payers actually operate at.
/// </summary>
public enum IdialLevel
{
    Unproofed = 0,
    Idial1 = 1,
    Idial2 = 2,
    Idial3 = 3,
    Idial4 = 4
}
