using System.Diagnostics;
using System.Text;
using TrustCartographer.UI.Live;
using Yarp.ReverseProxy.Model;

namespace TrustCartographer.Web.Services;

/// <summary>
/// Runs in the YARP proxy pipeline. Buffers the request body so the upstream
/// can still read it, swaps the response body so we can copy bytes as YARP
/// writes them, then records a <see cref="TrafficEntry"/> with both bodies
/// (truncated to <see cref="TrafficCaptureLimits.MaxBodyBytes"/>).
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

        // Buffer the request body so we can read it without consuming the stream
        // YARP needs to forward upstream.
        context.Request.EnableBuffering();
        var (requestBody, requestBytes, requestTruncated) = await ReadBodyAsync(
            context.Request.Body, context.Request.ContentType, context.RequestAborted);
        context.Request.Body.Position = 0;

        // Swap the response body for a buffer YARP writes into; we then copy
        // back to the original stream after capture.
        var originalResponseBody = context.Response.Body;
        await using var captureBuffer = new MemoryStream();
        context.Response.Body = captureBuffer;

        try
        {
            await _next(context);

            captureBuffer.Position = 0;
            var (responseBody, responseBytes, responseTruncated) = await ReadBodyAsync(
                captureBuffer, context.Response.ContentType, context.RequestAborted);

            captureBuffer.Position = 0;
            await captureBuffer.CopyToAsync(originalResponseBody, context.RequestAborted);

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
                DurationMs: sw.ElapsedMilliseconds,
                RequestContentType: context.Request.ContentType,
                RequestBody: requestBody,
                RequestBodyBytes: requestBytes,
                RequestBodyTruncated: requestTruncated,
                ResponseContentType: context.Response.ContentType,
                ResponseBody: responseBody,
                ResponseBodyBytes: responseBytes,
                ResponseBodyTruncated: responseTruncated));
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }
    }

    private static async Task<(string? Body, long Bytes, bool Truncated)> ReadBodyAsync(
        Stream stream, string? contentType, CancellationToken ct)
    {
        if (stream.CanSeek && stream.Length == 0) return (null, 0, false);

        // Read up to MaxBodyBytes + 1 so we can detect truncation cleanly.
        var maxRead = TrafficCaptureLimits.MaxBodyBytes + 1;
        var buf = new byte[maxRead];
        var total = 0;
        int n;
        while (total < maxRead && (n = await stream.ReadAsync(buf.AsMemory(total, maxRead - total), ct)) > 0)
            total += n;

        var truncated = total > TrafficCaptureLimits.MaxBodyBytes;
        var captured = truncated ? TrafficCaptureLimits.MaxBodyBytes : total;

        if (!IsTextLike(contentType))
            return (null, total, truncated);

        var body = Encoding.UTF8.GetString(buf, 0, captured);
        return (body, total, truncated);
    }

    private static bool IsTextLike(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;
        var ct = contentType.ToLowerInvariant();
        return ct.StartsWith("text/", StringComparison.Ordinal)
            || ct.Contains("json", StringComparison.Ordinal)
            || ct.Contains("xml", StringComparison.Ordinal)
            || ct.Contains("x-www-form-urlencoded", StringComparison.Ordinal)
            || ct.Contains("jwt", StringComparison.Ordinal);
    }

    private static string? InferServiceName(string? destinationAddress)
    {
        if (string.IsNullOrEmpty(destinationAddress)) return null;
        // Cluster destinations are addressed by exact service name (e.g. "https://csp-issuer"),
        // so the host IS the service name. Returning it verbatim keeps every service distinct —
        // payer-issuer / provider-issuer / csp-issuer / holder-minor / identity-csp no longer
        // collapse onto "issuer"/"holder", which the Live Flow lifelines depend on.
        if (Uri.TryCreate(destinationAddress, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
            return uri.Host.ToLowerInvariant();
        return null;
    }
}
