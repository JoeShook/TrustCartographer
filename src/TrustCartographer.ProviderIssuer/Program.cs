using Microsoft.FluentUI.AspNetCore.Components;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Services;
using TrustCartographer.Issuing;
using TrustCartographer.ProviderIssuer.Components;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Issuance kit services (singletons — issuer identity is process-stable).
builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<DidKeyService>();
builder.Services.AddSingleton<VcService>();
builder.Services.AddSingleton<TrustCartographer.Core.SdJwt.SdJwtService>();
builder.Services.AddSingleton<PopJwtService>();
builder.Services.AddSingleton<IssuerKeyMaterial>();
builder.Services.AddSingleton<IssuerStores>();
builder.Services.AddSingleton<CredentialCatalog>();
builder.Services.AddSingleton(new IssuerProfile
{
    Name = "Northwest Family Clinic (Provider)",
    Pathway = "B-in-person",
    PathwayLabel = "Pathway B — provider in-person photo ID"
});

// Pathway B: in-person photo-ID proofing is fixed for this issuer (no separate CSP).
builder.Services.AddSingleton<IAssuranceSource>(new StaticAssuranceSource(
    assuranceLevel: "IDIAL3",
    pathway: "B-in-person",
    method: "provider-in-person",
    basis: "Identity verified in person via government photo ID at the clinic."));

builder.Services.AddHttpClient("loopback");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();
app.MapDefaultEndpoints();

// Behind the TC.Web YARP proxy this service is mounted at /provider-issuer. Reinstate the
// stripped prefix from X-Forwarded-Prefix as PathBase so Blazor + assets resolve under it.
app.Use(async (context, next) =>
{
    var prefix = context.Request.Headers["X-Forwarded-Prefix"].FirstOrDefault();
    if (!string.IsNullOrEmpty(prefix))
    {
        context.Request.PathBase = prefix;
    }
    await next();
});

app.UseAntiforgery();
app.MapStaticAssets();

app.MapGet("/info", (IssuerKeyMaterial keys, IssuerProfile profile) => Results.Ok(new
{
    name = "provider-issuer",
    purpose = "Issues MinorIdentityCredential SD-JWT VCs (Pathway B — in-person)",
    phase = "B2",
    pathway = profile.Pathway,
    did = keys.Did
}));
app.MapGet("/ping", () => "pong");
app.MapOid4VciEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
