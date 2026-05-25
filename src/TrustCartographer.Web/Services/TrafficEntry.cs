namespace TrustCartographer.Web.Services;

/// <summary>
/// One captured request/response pair as observed by the YARP proxy.
/// Phase 1: method/URL/status/timing only. Phase 2 will add request/response bodies
/// (additive change — consumers should accept new fields).
/// </summary>
public record TrafficEntry(
    DateTimeOffset Timestamp,
    string From,
    string To,
    string Method,
    string Path,
    int? StatusCode,
    long DurationMs);
