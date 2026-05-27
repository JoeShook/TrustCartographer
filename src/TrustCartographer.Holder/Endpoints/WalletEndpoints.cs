using TrustCartographer.Holder.Services;

namespace TrustCartographer.Holder.Endpoints;

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

        app.MapGet("/wallet/credentials", (WalletStore store) => Results.Ok(store.All()));

        app.MapGet("/wallet/did", (HolderKeyMaterial keys) => Results.Ok(new { did = keys.Did }));
    }
}

public record RedeemOfferRequest(string OfferUri);
