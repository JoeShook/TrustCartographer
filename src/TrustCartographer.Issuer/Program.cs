using Microsoft.FluentUI.AspNetCore.Components;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Services;
using TrustCartographer.Issuer.Components;
using TrustCartographer.Issuing;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// OID4VCI services (singletons — issuer identity is process-stable)
builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<DidKeyService>();
builder.Services.AddSingleton<VcService>();
builder.Services.AddSingleton<TrustCartographer.Core.SdJwt.SdJwtService>();
builder.Services.AddSingleton<PopJwtService>();
builder.Services.AddSingleton<IssuerKeyMaterial>();
builder.Services.AddSingleton<IssuerStores>();
builder.Services.AddSingleton<CredentialCatalog>();

// Loopback HttpClient used by the Mint page's "Send to local Holder wallet"
// button — BaseAddress is set per-request from NavigationManager.BaseUri.
builder.Services.AddHttpClient("loopback");

// Blazor Server + FluentUI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();
app.MapDefaultEndpoints();

// Behind the TC.Web YARP proxy this service is mounted at /issuer. YARP strips
// that prefix and forwards it as X-Forwarded-Prefix; reinstate it as the request
// PathBase so the Blazor base href, static assets, the SignalR circuit, and all
// generated links resolve under /issuer/. Direct access (no header) leaves
// PathBase empty and everything resolves at the root.
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

// Diagnostics + OID4VCI endpoints (unchanged from Phase 2A)
app.MapGet("/info", (IssuerKeyMaterial keys) => Results.Ok(new
{
    name = "issuer",
    purpose = "Issues PractitionerLicenseCredential VCs via OID4VCI",
    phase = "2A.2",
    did = keys.Did
}));
app.MapGet("/ping", () => "pong");
app.MapOid4VciEndpoints();

// Blazor Server takes over GET / via Home.razor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
