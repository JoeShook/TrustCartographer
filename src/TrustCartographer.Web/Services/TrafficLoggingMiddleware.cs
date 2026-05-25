using System.Diagnostics;
using Yarp.ReverseProxy.Model;

namespace TrustCartographer.Web.Services;

/// <summary>
/// Runs in the YARP proxy pipeline. After the upstream request completes,
/// captures method/URL/status/duration into <see cref="TrafficLog"/>.
/// Phase 1: no body capture. Phase 2 will buffer request body + intercept response body.
/// </summary>
public sealed class TrafficLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TrafficLog _log;

    public TrafficLoggingMiddleware(RequestDelegate next, TrafficLog log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        var proxyFeature = context.Features.Get<IReverseProxyFeature>();
        var destinationAddress = proxyFeature?.ProxiedDestination?.Model?.Config?.Address;

        _log.Add(new TrafficEntry(
            Timestamp: DateTimeOffset.UtcNow,
            From: "client",
            To: InferServiceName(destinationAddress) ?? "unknown",
            Method: context.Request.Method,
            Path: context.Request.Path + context.Request.QueryString,
            StatusCode: context.Response.StatusCode,
            DurationMs: sw.ElapsedMilliseconds));
    }

    private static string? InferServiceName(string? destinationAddress)
    {
        if (string.IsNullOrEmpty(destinationAddress)) return null;
        var lower = destinationAddress.ToLowerInvariant();
        if (lower.Contains("issuer")) return "issuer";
        if (lower.Contains("holder")) return "holder";
        if (lower.Contains("verifier")) return "verifier";
        return null;
    }
}
