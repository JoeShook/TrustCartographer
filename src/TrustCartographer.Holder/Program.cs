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
