using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Services;
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

// The wallet does its own outbound HTTPS calls; service-discovery defaults
// from AddServiceDefaults apply so https://issuer etc. resolve to Aspire
// dev endpoints.
builder.Services.AddHttpClient<WalletClient>();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/info", (HolderKeyMaterial keys) => Results.Ok(new
{
    name = "holder",
    purpose = "Holds VCs; receives via OID4VCI, presents via OID4VP",
    phase = "2A",
    did = keys.Did
}));

app.MapGet("/ping", () => "pong");

app.MapGet("/", () => Results.Content(
    "<html><body style='font-family:sans-serif'><h1>TrustCartographer Holder Wallet</h1>" +
    "<h2>Wallet endpoints</h2><ul>" +
    "<li>POST /wallet/redeem-offer { offer_uri }</li>" +
    "<li><a href='/wallet/credentials'>/wallet/credentials</a></li>" +
    "<li><a href='/wallet/did'>/wallet/did</a></li>" +
    "</ul><h2>Diagnostics</h2><ul>" +
    "<li><a href='/info'>/info</a></li><li><a href='/ping'>/ping</a></li>" +
    "<li><a href='/health'>/health</a></li><li><a href='/alive'>/alive</a></li>" +
    "</ul></body></html>",
    "text/html"));

app.MapWalletEndpoints();

app.Run();
