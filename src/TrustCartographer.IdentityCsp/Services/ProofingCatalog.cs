using TrustCartographer.Core.Proofing;

namespace TrustCartographer.IdentityCsp.Services;

/// <summary>
/// The proofing methods this CSP offers and the FAST IDIAL each one yields. A real
/// CSP would run actual identity proofing; here each method maps to a representative
/// assurance level so the delegation math is visible.
/// </summary>
public sealed class ProofingCatalog
{
    public string DisplayName => "Identity CSP (Login.gov-style)";

    public IReadOnlyDictionary<string, ProofingMethod> Methods { get; } =
        new Dictionary<string, ProofingMethod>
        {
            ["login.gov-ial2"]     = new("login.gov-ial2", "Login.gov — remote IAL2", IdialLevel.Idial4),
            ["commercial-ial2"]    = new("commercial-ial2", "Commercial IAL2 CSP", IdialLevel.Idial4),
            ["provider-in-person"] = new("provider-in-person", "Provider in-person photo ID", IdialLevel.Idial3),
            ["payer-enrollment"]   = new("payer-enrollment", "Payer enrollment (documented, remote)", IdialLevel.Idial3),
            ["self-asserted"]      = new("self-asserted", "Self-asserted (no evidence)", IdialLevel.Idial1),
        };

    public IdialLevel IdialFor(string method) =>
        Methods.TryGetValue(method, out var m) ? m.Idial : IdialLevel.Unproofed;
}

public record ProofingMethod(string Id, string Label, IdialLevel Idial);
