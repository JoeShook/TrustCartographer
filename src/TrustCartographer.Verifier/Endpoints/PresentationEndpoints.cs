using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Oid4Vp;
using TrustCartographer.Core.SdJwt;
using TrustCartographer.Core.Services;
using TrustCartographer.Verifier.Services;

namespace TrustCartographer.Verifier.Endpoints;

/// <summary>
/// OID4VP 1.0 Verifier endpoints. The Verifier mints an authorization request
/// (presentation-request), the wallet dereferences it (presentation-request/{state}),
/// then posts the Verifiable Presentation back (presentation-response, direct_post).
/// Mirrors the Issuer's <c>Oid4VciEndpoints</c> so the protocol shape is readable
/// in one file.
/// </summary>
public static class PresentationEndpoints
{
    public static void MapPresentationEndpoints(this IEndpointRouteBuilder app)
    {
        // Mint an authorization request. The demo UI calls this to obtain a
        // request URI it then hands to the Holder wallet.
        app.MapPost("/presentation-request", (HttpContext ctx, VerifierStores stores, PresentationCatalog catalog,
            [FromBody] CreatePresentationRequestBody? body) =>
        {
            var definition = new PresentationDefinition
            {
                Id = $"pd-{Guid.NewGuid():N}",
                RequiredCredentialType = body?.CredentialType ?? PresentationCatalog.RequiredCredentialType,
                Purpose = catalog.Purpose,
                RequiredClaims = body?.RequiredClaims,
                Format = body?.Format
            };
            var pending = stores.CreateRequest(definition, TimeSpan.FromMinutes(5));
            var request = BuildRequest(ctx, pending);
            var requestUri = PresentationRequestUri.BuildByReference(
                $"{VerifierBaseUrl(ctx)}/presentation-request/{pending.State}");

            return Results.Ok(new CreatePresentationRequestResponse(request, requestUri, pending.State));
        });

        // By-reference dereference: the wallet fetches the full request object.
        app.MapGet("/presentation-request/{state}", (HttpContext ctx, string state, VerifierStores stores) =>
        {
            var pending = stores.GetRequest(state);
            return pending is null
                ? Results.NotFound(new { error = "unknown_request", detail = "No pending request with that state." })
                : Results.Ok(BuildRequest(ctx, pending));
        });

        // direct_post: the wallet returns the Verifiable Presentation here.
        app.MapPost("/presentation-response", (
            HttpContext ctx,
            VerifierStores stores,
            VpService vp,
            SdJwtService sdJwt,
            DidKeyService didKey,
            PresentationCatalog catalog,
            [FromBody] PresentationResponseSubmission body) =>
        {
            if (string.IsNullOrWhiteSpace(body.State) || string.IsNullOrWhiteSpace(body.VpToken))
                return Results.BadRequest(new { error = "invalid_request", detail = "vp_token and state are required." });

            var pending = stores.GetRequest(body.State);
            if (pending is null)
                return Results.BadRequest(new { error = "invalid_request", detail = "Unknown or already-completed state." });

            if (!stores.ConsumeNonce(pending.Nonce))
            {
                stores.Complete(body.State);
                return Reject(stores, "Nonce was unknown, expired, or already used.");
            }

            stores.Complete(body.State);

            // An SD-JWT presentation (contains '~') is verified with SdJwtService; a plain
            // VP-JWT goes through VpService. Both resolve the issuer key from its did:key.
            PresentationResponseResult outcome;
            if (body.VpToken.Contains(SdJwtConstants.Delimiter))
            {
                outcome = VerifySdJwt(body.VpToken, VerifierBaseUrl(ctx), pending, sdJwt, didKey);
            }
            else
            {
                var result = vp.VerifyPresentation(
                    body.VpToken,
                    expectedAudience: VerifierBaseUrl(ctx),
                    expectedNonce: pending.Nonce,
                    requiredCredentialType: pending.Definition.RequiredCredentialType);
                outcome = new PresentationResponseResult(
                    result.Valid ? "accepted" : "rejected", result.Message,
                    result.HolderDid, result.IssuerDid, result.CredentialType);
            }

            stores.RecordVerification(new VerificationRecord(
                At: DateTimeOffset.UtcNow,
                Status: outcome.Status,
                HolderDid: outcome.HolderDid,
                IssuerDid: outcome.IssuerDid,
                CredentialType: outcome.CredentialType,
                Reason: outcome.Reason));

            return Results.Ok(outcome);
        });
    }

    private static PresentationResponseResult VerifySdJwt(
        string vpToken, string audience, PendingRequest pending, SdJwtService sdJwt, DidKeyService didKey)
    {
        string issuerDid;
        try
        {
            var issuerJwt = vpToken.Split(SdJwtConstants.Delimiter)[0];
            using var doc = System.Text.Json.JsonDocument.Parse(Base64Url.DecodeToString(issuerJwt.Split('.')[1]));
            issuerDid = doc.RootElement.GetProperty("iss").GetString() ?? "";
        }
        catch (Exception ex)
        {
            return new PresentationResponseResult("rejected", $"Could not read SD-JWT issuer: {ex.Message}", null, null, null);
        }

        byte[] issuerPublicKey;
        try { issuerPublicKey = didKey.DecodeEd25519(issuerDid); }
        catch (Exception ex)
        {
            return new PresentationResponseResult("rejected", $"Could not resolve issuer DID {issuerDid}: {ex.Message}", null, issuerDid, null);
        }

        var r = sdJwt.Verify(vpToken, issuerPublicKey, audience, pending.Nonce, pending.Definition.RequiredClaims);
        return new PresentationResponseResult(r.Valid ? "accepted" : "rejected", r.Message, r.HolderDid, r.IssuerDid, r.Vct);
    }

    private static IResult Reject(VerifierStores stores, string reason)
    {
        stores.RecordVerification(new VerificationRecord(
            DateTimeOffset.UtcNow, "rejected", null, null, null, reason));
        return Results.Ok(new PresentationResponseResult("rejected", reason, null, null, null));
    }

    private static PresentationRequest BuildRequest(HttpContext ctx, PendingRequest pending)
    {
        var clientId = VerifierBaseUrl(ctx);
        return new PresentationRequest
        {
            ClientId = clientId,
            Nonce = pending.Nonce,
            ResponseUri = $"{clientId}/presentation-response",
            State = pending.State,
            PresentationDefinition = pending.Definition
        };
    }

    private static string VerifierBaseUrl(HttpContext ctx)
    {
        // YARP forwards under /verifier; honour the original path-base so URLs in
        // the request object round-trip correctly through the proxy.
        var req = ctx.Request;
        var pathBase = ctx.Request.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? string.Empty;
        return $"{req.Scheme}://{req.Host}{pathBase}";
    }
}

public record CreatePresentationRequestResponse(
    PresentationRequest Request,
    string RequestUri,
    string State);

public record CreatePresentationRequestBody
{
    [JsonPropertyName("credentialType")] public string? CredentialType { get; init; }
    [JsonPropertyName("requiredClaims")] public IReadOnlyList<string>? RequiredClaims { get; init; }
    [JsonPropertyName("format")] public string? Format { get; init; }
}

public record PresentationResponseSubmission
{
    [JsonPropertyName("vp_token")]
    public string VpToken { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("presentation_submission")]
    public object? PresentationSubmission { get; init; }
}

public record PresentationResponseResult(
    string Status,
    string Reason,
    string? HolderDid,
    string? IssuerDid,
    string? CredentialType);
