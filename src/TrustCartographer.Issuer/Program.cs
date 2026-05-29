using Microsoft.FluentUI.AspNetCore.Components;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Services;
using TrustCartographer.Issuer.Components;
using TrustCartographer.Issuer.Endpoints;
using TrustCartographer.Issuer.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// OID4VCI services (singletons — issuer identity is process-stable)
builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<DidKeyService>();
builder.Services.AddSingleton<VcService>();
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
