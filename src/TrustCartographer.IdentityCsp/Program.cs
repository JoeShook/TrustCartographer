using Microsoft.FluentUI.AspNetCore.Components;
using TrustCartographer.Core.Proofing;
using TrustCartographer.IdentityCsp.Components;
using TrustCartographer.IdentityCsp.Endpoints;
using TrustCartographer.IdentityCsp.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Delegated-proofing services.
builder.Services.AddSingleton<DelegationPolicy>();
builder.Services.AddSingleton<ProofingCatalog>();
builder.Services.AddSingleton<CspStores>();

// Blazor Server + FluentUI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();
app.MapDefaultEndpoints();

// Behind the TC.Web YARP proxy this service is mounted at /identity-csp. Reinstate the
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

app.MapGet("/info", (ProofingCatalog catalog) => Results.Ok(new
{
    name = "identity-csp",
    purpose = "Delegated identity proofing → FAST IDIAL",
    phase = "B1",
    displayName = catalog.DisplayName
}));
app.MapGet("/ping", () => "pong");
app.MapProofingEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
