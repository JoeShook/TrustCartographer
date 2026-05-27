namespace TrustCartographer.UI.Live;

/// <summary>
/// One captured request/response pair. Defined here in the RCL so the LiveFlow
/// page can render it without taking a dependency on the YARP-bound host.
/// </summary>
public record TrafficEntry(
    DateTimeOffset Timestamp,
    string From,
    string To,
    string Method,
    string Path,
    int? StatusCode,
    long DurationMs,
    string? RequestContentType,
    string? RequestBody,
    long RequestBodyBytes,
    bool RequestBodyTruncated,
    string? ResponseContentType,
    string? ResponseBody,
    long ResponseBodyBytes,
    bool ResponseBodyTruncated);

/// <summary>
/// Visualization-only event source for captured proxy traffic. The TC.Web host
/// implements this with a YARP middleware; MAUI heads can supply a no-op or a
/// pull-based variant.
/// </summary>
public interface ITrafficLog
{
    event Action<TrafficEntry>? OnNewEntry;
    IReadOnlyList<TrafficEntry> Snapshot();
    void Clear();
}
