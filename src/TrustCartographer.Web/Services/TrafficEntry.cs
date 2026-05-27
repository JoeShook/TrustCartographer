namespace TrustCartographer.Web.Services;

/// <summary>
/// Per-capture sizing limits enforced by <see cref="TrafficLoggingMiddleware"/>.
/// The TrafficEntry record itself lives in TrustCartographer.UI.Live so the
/// LiveFlow page can render it without referencing the host.
/// </summary>
public static class TrafficCaptureLimits
{
    public const int MaxBodyBytes = 64 * 1024;
}
