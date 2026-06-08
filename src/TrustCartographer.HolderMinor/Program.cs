using Microsoft.FluentUI.AspNetCore.Components;
using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Oid4Vp;
using TrustCartographer.Core.Services;
using TrustCartographer.HolderMinor.Components;
using TrustCartographer.Wallet;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<DidKeyService>();
builder.Services.AddSingleton<VcService>();
builder.Services.AddSingleton<PopJwtService>();
builder.Services.AddSingleton<HolderKeyMaterial>();
builder.Services.AddSingleton<WalletStore>();
builder.Services.AddSingleton<VpService>();
builder.Services.AddSingleton<TrustCartographer.Core.SdJwt.SdJwtService>();

// Wallet clients use outbound HttpClients for OID4VCI (issuance) and OID4VP (presentation)
builder.Services.AddHttpClient<WalletClient>();
builder.Services.AddHttpClient<PresentationClient>();
builder.Services.AddHttpClient<CustodyClient>();   // required by the shared wallet endpoints (transfer-custody)

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();
app.MapDefaultEndpoints();

// Behind the TC.Web YARP proxy this service is mounted at /holder-minor. Reinstate the
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

app.MapGet("/info", (HolderKeyMaterial keys) => Results.Ok(new
{
    name = "holder-minor",
    purpose = "The minor's own wallet; receives credentials via custody transfer (re-issuance)",
    phase = "B3",
    did = keys.Did
}));
app.MapGet("/ping", () => "pong");
app.MapWalletEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
