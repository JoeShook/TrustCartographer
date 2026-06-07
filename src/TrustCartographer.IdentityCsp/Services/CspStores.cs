using System.Collections.Concurrent;
using System.Security.Cryptography;
using TrustCartographer.Core.Proofing;

namespace TrustCartographer.IdentityCsp.Services;

/// <summary>
/// In-memory record of delegated proofing events, keyed by id so issuers can later
/// consult one, plus a ring-buffered log for the Activity page. Process-lifetime only.
/// </summary>
public sealed class CspStores
{
    private const int MaxLog = 50;
    private readonly ConcurrentDictionary<string, ProofingEvent> _events = new();
    private readonly ConcurrentQueue<ProofingEvent> _log = new();

    public ProofingEvent Record(string delegateId, string method, IdialLevel delegateIdial, DelegationResult result)
    {
        var ev = new ProofingEvent(
            NewId(), delegateId, method, delegateIdial,
            result.MinorEffectiveIdial, result.Issuable, result.Reason, DateTimeOffset.UtcNow);
        _events[ev.Id] = ev;
        _log.Enqueue(ev);
        while (_log.Count > MaxLog && _log.TryDequeue(out _)) { }
        return ev;
    }

    public ProofingEvent? Get(string id) => _events.TryGetValue(id, out var e) ? e : null;

    public IReadOnlyList<ProofingEvent> Log() => _log.Reverse().ToArray();

    private static string NewId()
    {
        Span<byte> b = stackalloc byte[12];
        RandomNumberGenerator.Fill(b);
        return "pe-" + Convert.ToHexString(b).ToLowerInvariant();
    }
}
