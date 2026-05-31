using Microsoft.FluentUI.AspNetCore.Components;
using TrustCartographer.Core.Oid4Vp;
using TrustCartographer.Core.Services;
using TrustCartographer.Verifier.Components;
using TrustCartographer.Verifier.Endpoints;
using TrustCartographer.Verifier.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// OID4VP verification services.
builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<DidKeyService>();
builder.Services.AddSingleton<VcService>();
builder.Services.AddSingleton<VpService>();
builder.Services.AddSingleton<VerifierStores>();
builder.Services.AddSingleton<PresentationCatalog>();

// Loopback HttpClient used by the Request page's "Send to local Holder wallet"
// button — BaseAddress is set per-request from NavigationManager.BaseUri.
builder.Services.AddHttpClient("loopback");

// Blazor Server + FluentUI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();
app.MapDefaultEndpoints();

// Behind the TC.Web YARP proxy this service is mounted at /verifier. YARP strips
// that prefix and forwards it as X-Forwarded-Prefix; reinstate it as the request
// PathBase so the Blazor base href, static assets, the SignalR circuit, and all
// generated links resolve under /verifier/. Direct access (no header) leaves
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

// Diagnostics + OID4VP endpoints
app.MapGet("/info", (PresentationCatalog catalog) => Results.Ok(new
{
    name = "verifier",
    purpose = "Requests + verifies VPs via OID4VP",
    phase = "2B",
    requires = PresentationCatalog.RequiredCredentialType,
    displayName = catalog.DisplayName
}));
app.MapGet("/ping", () => "pong");
app.MapPresentationEndpoints();

// Blazor Server takes over GET / via Home.razor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
