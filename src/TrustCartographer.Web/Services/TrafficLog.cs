using System.Collections.Concurrent;
using TrustCartographer.UI.Live;

namespace TrustCartographer.Web.Services;

/// <summary>
/// In-memory ring buffer of captured proxy traffic, for visualization only.
/// No audit, no persistence, no external sinks. The Live Flow page subscribes
/// to <see cref="OnNewEntry"/> to render exchanges in real time.
/// </summary>
public sealed class TrafficLog : ITrafficLog
{
    private const int MaxEntries = 500;
    private readonly ConcurrentQueue<TrafficEntry> _entries = new();

    public event Action<TrafficEntry>? OnNewEntry;

    public IReadOnlyList<TrafficEntry> Snapshot() => _entries.ToArray();

    public void Add(TrafficEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _)) { }
        OnNewEntry?.Invoke(entry);
    }

    public void Clear()
    {
        while (_entries.TryDequeue(out _)) { }
    }
}
