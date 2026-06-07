using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.SdJwt;
using TrustCartographer.Core.Services;

namespace TrustCartographer.Issuing;

/// <summary>
/// Maps the OID4VCI 1.0 endpoints onto the Issuer's pipeline. Each handler is
/// intentionally small and self-contained so the protocol shape (metadata →
/// offer → token → nonce → credential) is readable in one file.
/// </summary>
public static class Oid4VciEndpoints
{
    public static void MapOid4VciEndpoints(this IEndpointRouteBuilder app)
    {
        MapMetadata(app);
        MapCredentialOffer(app);
        MapToken(app);
        MapNonce(app);
        MapCredential(app);
    }

    // ───────────────────────── Metadata ─────────────────────────

    private static void MapMetadata(IEndpointRouteBuilder app)
    {
        app.MapGet(Oid4VciConstants.IssuerMetadataPath, (HttpContext ctx, CredentialCatalog catalog) =>
        {
            var issuerUrl = IssuerBaseUrl(ctx);
            return Results.Ok(new IssuerMetadata
            {
                CredentialIssuer = issuerUrl,
                AuthorizationServers = [issuerUrl],
                CredentialEndpoint = $"{issuerUrl}/credential",
                NonceEndpoint = $"{issuerUrl}/nonce",
                CredentialConfigurationsSupported = catalog.SupportedConfigurations,
                Display =
                [
                    new DisplayInfo { Name = "State Medical Board (TrustCartographer demo)", Locale = "en-US" }
                ]
            });
        });

        app.MapGet(Oid4VciConstants.AuthorizationServerMetadataPath, (HttpContext ctx) =>
        {
            var issuerUrl = IssuerBaseUrl(ctx);
            return Results.Ok(new AuthorizationServerMetadata
            {
                Issuer = issuerUrl,
                TokenEndpoint = $"{issuerUrl}/token",
                GrantTypesSupported = [Oid4VciConstants.PreAuthorizedCodeGrantType],
                PreAuthorizedGrantAnonymousAccessSupported = true
            });
        });
    }

    // ───────────────────────── Credential Offer ─────────────────────────

    private static void MapCredentialOffer(IEndpointRouteBuilder app)
    {
        // Admin-style mint endpoint. The demo UI calls this to obtain an offer
        // URI it then hands to the Holder wallet.
        app.MapPost("/credential-offer", (
            HttpContext ctx,
            CredentialCatalog catalog,
            IssuerStores stores,
            [FromBody] CreateOfferRequest? body) =>
        {
            var configId = body?.CredentialConfigurationId ?? CredentialCatalog.PractitionerLicenseId;
            if (!catalog.SupportedConfigurations.ContainsKey(configId))
                return Results.BadRequest(new OAuthErrorResponse { Error = "unsupported_credential_type" });

            var claims = body?.SubjectClaims is { Count: > 0 }
                ? body.SubjectClaims
                : catalog.DefaultSubjectClaimsFor(configId);

            var record = stores.CreateOffer(configId, claims);

            var offer = new CredentialOffer
            {
                CredentialIssuer = IssuerBaseUrl(ctx),
                CredentialConfigurationIds = [configId],
                Grants = new CredentialOfferGrants
                {
                    PreAuthorizedCode = new PreAuthorizedCodeGrant
                    {
                        PreAuthorizedCode = record.PreAuthorizedCode
                    }
                }
            };

            return Results.Ok(new CreateOfferResponse(
                Offer: offer,
                OfferUri: CredentialOfferUri.BuildByValue(offer),
                OfferId: record.OfferId));
        });
    }

    // ───────────────────────── Token ─────────────────────────

    private static void MapToken(IEndpointRouteBuilder app)
    {
        app.MapPost("/token", async (HttpContext ctx, IssuerStores stores) =>
        {
            if (!ctx.Request.HasFormContentType)
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_request",
                    ErrorDescription = "Token endpoint expects application/x-www-form-urlencoded."
                });

            var form = await ctx.Request.ReadFormAsync();
            var grantType = form["grant_type"].ToString();
            if (grantType != Oid4VciConstants.PreAuthorizedCodeGrantType)
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "unsupported_grant_type",
                    ErrorDescription = $"Only {Oid4VciConstants.PreAuthorizedCodeGrantType} is supported."
                });

            var preAuthCode = form["pre-authorized_code"].ToString();
            if (string.IsNullOrEmpty(preAuthCode))
                return Results.BadRequest(new OAuthErrorResponse { Error = "invalid_request", ErrorDescription = "pre-authorized_code is required." });

            var offer = stores.RedeemOffer(preAuthCode);
            if (offer is null)
                return Results.BadRequest(new OAuthErrorResponse { Error = "invalid_grant", ErrorDescription = "Unknown or already-redeemed pre-authorized_code." });

            var token = stores.IssueAccessToken(offer, TimeSpan.FromMinutes(10));

            return Results.Ok(new TokenResponse
            {
                AccessToken = token.AccessToken,
                TokenType = "Bearer",
                ExpiresIn = (int)(token.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds,
                AuthorizationDetails =
                [
                    new AuthorizationDetail
                    {
                        Type = "openid_credential",
                        CredentialConfigurationId = offer.CredentialConfigurationId
                    }
                ]
            });
        });
    }

    // ───────────────────────── Nonce ─────────────────────────

    private static void MapNonce(IEndpointRouteBuilder app)
    {
        app.MapPost("/nonce", (IssuerStores stores) =>
        {
            var nonce = stores.MintNonce(TimeSpan.FromMinutes(5));
            return Results.Ok(new NonceResponse { CNonce = nonce });
        });
    }

    // ───────────────────────── Credential ─────────────────────────

    private static void MapCredential(IEndpointRouteBuilder app)
    {
        app.MapPost("/credential", async (
            HttpContext ctx,
            IssuerStores stores,
            IssuerKeyMaterial keys,
            CredentialCatalog catalog,
            PopJwtService pop,
            VcService vc,
            DidKeyService didKey,
            SdJwtService sdJwt,
            [FromBody] CredentialRequest body) =>
        {
            // 1. Bearer access token
            var token = ExtractBearer(ctx);
            if (token is null) return Unauthorized("invalid_token", "Missing bearer access token.");
            var session = stores.GetAccessToken(token);
            if (session is null) return Unauthorized("invalid_token", "Unknown or expired access token.");

            // 2. Resolve which credential to mint
            var configId = body.CredentialConfigurationId ?? session.CredentialConfigurationId;
            if (configId != session.CredentialConfigurationId)
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_credential_request",
                    ErrorDescription = "credential_configuration_id does not match the access token."
                });

            // 3. Proof-of-possession (jwt proof type, single proof in Phase 2A)
            var proofJwt = body.Proofs?.Jwt?.FirstOrDefault();
            if (string.IsNullOrEmpty(proofJwt))
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_proof",
                    ErrorDescription = "proofs.jwt[0] is required."
                });

            var nonceFromProof = TryExtractNonce(proofJwt);
            if (nonceFromProof is null || !stores.ConsumeNonce(nonceFromProof))
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_proof",
                    ErrorDescription = "Proof nonce is missing, unknown, or already used. Call /nonce first."
                });

            var audience = IssuerBaseUrl(ctx);
            var verification = pop.Verify(proofJwt, audience, nonceFromProof);
            if (!verification.Valid || verification.HolderDidKey is null)
                return Results.BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_proof",
                    ErrorDescription = verification.Message
                });

            // 4. Issue the credential bound to the holder's DID. The format the catalog
            //    advertises decides JWT-VC vs SD-JWT VC. For SD-JWT the holder key (from
            //    the PoP proof) becomes the cnf key binding.
            var config = catalog.SupportedConfigurations[configId];
            string credentialString;
            string credentialId;
            if (config.Format == SdJwtConstants.SdJwtVcType)
            {
                var holderPublicKey = didKey.DecodeEd25519(verification.HolderDidKey);
                var sdIssued = sdJwt.IssueSdJwtVc(
                    issuerDid: keys.Did,
                    issuerVerificationMethodId: keys.VerificationMethodId,
                    issuerPrivateKey: keys.KeyPair.PrivateKey,
                    vct: catalog.CredentialTypeFor(configId),
                    selectivelyDisclosableClaims: session.SubjectClaims,
                    holderCnfJwk: Jwk.FromEd25519PublicKey(holderPublicKey),
                    validity: TimeSpan.FromDays(365));
                credentialString = sdIssued.Combined;
                credentialId = $"urn:uuid:{Guid.NewGuid()}";
            }
            else
            {
                var issued = vc.IssueJwtVc(
                    issuerDid: keys.Did,
                    issuerVerificationMethodId: keys.VerificationMethodId,
                    issuerPrivateKey: keys.KeyPair.PrivateKey,
                    subjectDid: verification.HolderDidKey,
                    credentialType: catalog.CredentialTypeFor(configId),
                    claims: session.SubjectClaims,
                    validity: TimeSpan.FromDays(730));
                credentialString = issued.Jwt;
                credentialId = issued.CredentialId;
            }

            stores.RecordIssuance(new IssuanceRecord(
                At: DateTimeOffset.UtcNow,
                SubjectDid: verification.HolderDidKey,
                CredentialConfigurationId: configId,
                CredentialId: credentialId));

            return Results.Ok(new CredentialResponse
            {
                Credentials = [new IssuedCredential { Credential = credentialString }]
            });
        });
    }

    // ───────────────────────── Helpers ─────────────────────────

    private static string IssuerBaseUrl(HttpContext ctx)
    {
        // YARP forwards under /issuer; honour the original path-base so URLs in
        // metadata round-trip correctly through the proxy.
        var req = ctx.Request;
        var pathBase = ctx.Request.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? string.Empty;
        return $"{req.Scheme}://{req.Host}{pathBase}";
    }

    private static string? ExtractBearer(HttpContext ctx)
    {
        var header = ctx.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        return header.StartsWith(prefix, StringComparison.Ordinal) ? header[prefix.Length..] : null;
    }

    private static IResult Unauthorized(string error, string description) =>
        Results.Json(new OAuthErrorResponse { Error = error, ErrorDescription = description }, statusCode: StatusCodes.Status401Unauthorized);

    private static string? TryExtractNonce(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) return null;
        try
        {
            var payloadJson = Base64Url.DecodeToString(parts[1]);
            using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
            return doc.RootElement.TryGetProperty("nonce", out var n) && n.ValueKind == System.Text.Json.JsonValueKind.String
                ? n.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }
}

public record CreateOfferRequest(
    string? CredentialConfigurationId,
    IReadOnlyDictionary<string, object?>? SubjectClaims);

public record CreateOfferResponse(
    CredentialOffer Offer,
    string OfferUri,
    string OfferId);
