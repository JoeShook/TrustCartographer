using Microsoft.AspNetCore.Mvc;
using TrustCartographer.Core.Proofing;
using TrustCartographer.IdentityCsp.Services;

namespace TrustCartographer.IdentityCsp.Endpoints;

/// <summary>
/// Delegated identity-proofing endpoints. A responsible adult is proofed by some
/// method → a FAST IDIAL; the delegation policy (ceiling + discount + floor) then
/// derives the minor's effective assurance. The resulting proofing event is what an
/// issuer consults so the assurance basis travels in the credential.
/// </summary>
public static class ProofingEndpoints
{
    public static void MapProofingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/methods", (ProofingCatalog catalog) => Results.Ok(catalog.Methods.Values));

        app.MapPost("/proofing-event", (
            CspStores stores, ProofingCatalog catalog, DelegationPolicy policy,
            [FromBody] ProofingEventRequest body) =>
        {
            if (string.IsNullOrWhiteSpace(body.Method) || !catalog.Methods.ContainsKey(body.Method))
                return Results.BadRequest(new { error = "unknown_method", detail = "Unknown proofing method." });

            var delegateIdial = catalog.IdialFor(body.Method);
            var result = policy.Evaluate(delegateIdial);
            var ev = stores.Record(body.DelegateId ?? "delegate", body.Method, delegateIdial, result);
            return Results.Ok(ev);
        });

        app.MapGet("/proofing-event/{id}", (string id, CspStores stores) =>
        {
            var ev = stores.Get(id);
            return ev is null
                ? Results.NotFound(new { error = "unknown_proofing_event" })
                : Results.Ok(ev);
        });
    }
}

public record ProofingEventRequest(string? DelegateId, string Method);
