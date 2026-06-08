using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace TrustCartographer.Wallet;

/// <summary>
/// Wallet HTTP surface. <c>/wallet/redeem-offer</c> kicks off the full OID4VCI
/// dance against whatever Issuer the offer points at; the rest is read-only
/// introspection so demos can see what the wallet currently holds.
/// </summary>
public static class WalletEndpoints
{
    public static void MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/wallet/redeem-offer", async (
            RedeemOfferRequest body,
            WalletClient wallet,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.OfferUri))
                return Results.BadRequest(new { error = "offer_uri is required." });
            try
            {
                var stored = await wallet.RedeemOfferAsync(body.OfferUri, ct);
                return Results.Ok(stored);
            }
            catch (HttpRequestException ex)
            {
                return Results.Json(new { error = "issuance_failed", detail = ex.Message }, statusCode: 502);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = "invalid_offer", detail = ex.Message });
            }
        });

        // OID4VP: present a held credential to a Verifier and relay the verdict.
        app.MapPost("/wallet/present", async (
            PresentRequest body,
            PresentationClient presentation,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.RequestUri))
                return Results.BadRequest(new { error = "request_uri is required." });
            try
            {
                var outcome = await presentation.PresentAsync(body.RequestUri, ct);
                return Results.Ok(outcome);
            }
            catch (HttpRequestException ex)
            {
                return Results.Json(new { error = "presentation_failed", detail = ex.Message }, statusCode: 502);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = "invalid_presentation", detail = ex.Message });
            }
        });

        // Custody transfer: re-issue a guardian-held credential to the minor's own wallet.
        app.MapPost("/wallet/transfer-custody", async (
            CustodyTransferRequest body,
            CustodyClient custody,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.CredentialId) || string.IsNullOrWhiteSpace(body.MinorRedeemUrl))
                return Results.BadRequest(new { error = "credentialId and minorRedeemUrl are required." });
            try
            {
                var result = await custody.TransferCustodyAsync(body.CredentialId, body.MinorRedeemUrl, ct);
                return Results.Ok(result);
            }
            catch (HttpRequestException ex)
            {
                return Results.Json(new { error = "custody_transfer_failed", detail = ex.Message }, statusCode: 502);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = "custody_transfer_invalid", detail = ex.Message });
            }
        });

        app.MapGet("/wallet/credentials", (WalletStore store) => Results.Ok(store.All()));

        app.MapGet("/wallet/did", (HolderKeyMaterial keys) => Results.Ok(new { did = keys.Did }));
    }
}

public record RedeemOfferRequest(string OfferUri);

public record PresentRequest(string RequestUri);
