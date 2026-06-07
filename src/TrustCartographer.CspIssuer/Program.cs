using Microsoft.FluentUI.AspNetCore.Components;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Services;
using TrustCartographer.CspIssuer.Components;
using TrustCartographer.CspIssuer.Services;
using TrustCartographer.Issuing;

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
    Name = "Statewide IAL2 CSP Issuer",
    Pathway = "C-ial2-csp",
    PathwayLabel = "Pathway C — IAL2 CSP (delegated proofing)"
});

// Pathway C: assurance is derived per-offer by consulting the Identity-CSP's proofing event.
// Typed HttpClient so the consultation hop goes out over HTTP (captured by the proxy).
builder.Services.AddHttpClient<IAssuranceSource, CspAssuranceSource>();
builder.Services.AddHttpClient("loopback");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();
app.MapDefaultEndpoints();

// Behind the TC.Web YARP proxy this service is mounted at /csp-issuer. Reinstate the
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
    name = "csp-issuer",
    purpose = "Issues MinorIdentityCredential SD-JWT VCs (Pathway C — IAL2 CSP delegated proofing)",
    phase = "B2",
    pathway = profile.Pathway,
    did = keys.Did
}));
app.MapGet("/ping", () => "pong");
app.MapOid4VciEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
