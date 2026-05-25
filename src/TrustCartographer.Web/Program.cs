using Microsoft.FluentUI.AspNetCore.Components;
using TrustCartographer.Core.Services;
using TrustCartographer.Web.Components;
using TrustCartographer.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults: OTel, resilience, service discovery, health checks.
builder.AddServiceDefaults();

// Core services (existing).
builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<DidKeyService>();
builder.Services.AddSingleton<VcService>();

// Visualization-only traffic capture.
builder.Services.AddSingleton<TrafficLog>();

// YARP reverse proxy in front of Issuer / Holder / Verifier.
// Service-discovery destination resolver lets "http://issuer" etc. resolve
// to the actual Aspire-assigned port at runtime.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// Blazor Server + Fluent UI.
builder.Services.AddFluentUIComponents();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

// Health checks + /alive (from ServiceDefaults).
app.MapDefaultEndpoints();

// Phase 1 debug endpoint — returns the most recent captured exchanges as JSON.
// The proper Live Flow page replaces this in a follow-on.
app.MapGet("/traffic", (TrafficLog log) => Results.Ok(log.Snapshot()));

// YARP routes — owns /issuer/*, /holder/*, /verifier/* before Blazor's NotFound catches them.
app.MapReverseProxy(pipeline =>
{
    pipeline.UseMiddleware<TrafficLoggingMiddleware>();
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(TrustCartographer.UI.Routes).Assembly);

app.Run();
