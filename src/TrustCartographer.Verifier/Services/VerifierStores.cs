using System.Collections.Concurrent;
using System.Security.Cryptography;
using TrustCartographer.Core.Oid4Vp;

namespace TrustCartographer.Verifier.Services;

/// <summary>
/// In-memory state for the Verifier: pending presentation requests keyed by their
/// <c>state</c>, single-use nonces, and a ring-buffered verification log. Mirrors
/// the Issuer's <c>IssuerStores</c>; process-lifetime only — the visualisation
/// needs minutes, and persistence would obscure the protocol mechanics.
/// </summary>
public sealed class VerifierStores
{
    private const int MaxLogEntries = 50;
    private readonly ConcurrentDictionary<string, PendingRequest> _pending = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _activeNonces = new();
    private readonly ConcurrentQueue<VerificationRecord> _log = new();

    public PendingRequest CreateRequest(PresentationDefinition definition, TimeSpan nonceTtl)
    {
        var state = NewToken(16);
        var nonce = NewToken(16);
        _activeNonces[nonce] = DateTimeOffset.UtcNow + nonceTtl;
        var record = new PendingRequest(state, nonce, definition, DateTimeOffset.UtcNow);
        _pending[state] = record;
        return record;
    }

    public PendingRequest? GetRequest(string state) =>
        _pending.TryGetValue(state, out var r) ? r : null;

    /// <summary>Consume the request's nonce — single use. Returns true if it was valid.</summary>
    public bool ConsumeNonce(string nonce)
    {
        if (!_activeNonces.TryRemove(nonce, out var expiresAt)) return false;
        return expiresAt > DateTimeOffset.UtcNow;
    }

    public void Complete(string state) => _pending.TryRemove(state, out _);

    public void RecordVerification(VerificationRecord record)
    {
        _log.Enqueue(record);
        while (_log.Count > MaxLogEntries && _log.TryDequeue(out _)) { }
    }

    /// <summary>Snapshot of recent verifications, newest first.</summary>
    public IReadOnlyList<VerificationRecord> VerificationLog() => _log.Reverse().ToArray();

    private static string NewToken(int byteLen)
    {
        Span<byte> buf = stackalloc byte[byteLen];
        RandomNumberGenerator.Fill(buf);
        return Convert.ToBase64String(buf).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

public record PendingRequest(
    string State,
    string Nonce,
    PresentationDefinition Definition,
    DateTimeOffset CreatedAt);

public record VerificationRecord(
    DateTimeOffset At,
    string Status,            // "accepted" | "rejected"
    string? HolderDid,
    string? IssuerDid,
    string? CredentialType,
    string Reason);
