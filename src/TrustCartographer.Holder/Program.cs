using Microsoft.FluentUI.AspNetCore.Components;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Services;
using TrustCartographer.Holder.Components;
using TrustCartographer.Holder.Endpoints;
using TrustCartographer.Holder.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<DidKeyService>();
builder.Services.AddSingleton<VcService>();
builder.Services.AddSingleton<PopJwtService>();
builder.Services.AddSingleton<HolderKeyMaterial>();
builder.Services.AddSingleton<WalletStore>();

// Wallet client uses an outbound HttpClient for OID4VCI calls
builder.Services.AddHttpClient<WalletClient>();

// Blazor Server + FluentUI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();
app.MapDefaultEndpoints();

// Behind the TC.Web YARP proxy this service is mounted at /holder. YARP strips
// that prefix and forwards it as X-Forwarded-Prefix; reinstate it as the request
// PathBase so the Blazor base href, static assets, the SignalR circuit, and all
// generated links resolve under /holder/. Direct access (no header) leaves
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

// Diagnostics + wallet endpoints (unchanged from Phase 2A)
app.MapGet("/info", (HolderKeyMaterial keys) => Results.Ok(new
{
    name = "holder",
    purpose = "Holds VCs; receives via OID4VCI, presents via OID4VP",
    phase = "2A.2",
    did = keys.Did
}));
app.MapGet("/ping", () => "pong");
app.MapWalletEndpoints();

// Blazor Server takes over GET / via Home.razor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
