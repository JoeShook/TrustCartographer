using TrustCartographer.Core.Oid4Vci;
using TrustCartographer.Core.Services;
using TrustCartographer.Issuer.Endpoints;
using TrustCartographer.Issuer.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<DidKeyService>();
builder.Services.AddSingleton<VcService>();
builder.Services.AddSingleton<PopJwtService>();
builder.Services.AddSingleton<IssuerKeyMaterial>();
builder.Services.AddSingleton<IssuerStores>();
builder.Services.AddSingleton<CredentialCatalog>();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/info", (IssuerKeyMaterial keys) => Results.Ok(new
{
    name = "issuer",
    purpose = "Issues PractitionerLicenseCredential VCs via OID4VCI",
    phase = "2A",
    did = keys.Did
}));

app.MapGet("/ping", () => "pong");

app.MapGet("/", () => Results.Content(
    "<html><body style='font-family:sans-serif'><h1>TrustCartographer Issuer</h1>" +
    "<h2>OID4VCI 1.0 endpoints</h2><ul>" +
    "<li><a href='/.well-known/openid-credential-issuer'>/.well-known/openid-credential-issuer</a></li>" +
    "<li><a href='/.well-known/oauth-authorization-server'>/.well-known/oauth-authorization-server</a></li>" +
    "<li>POST /credential-offer (admin mint)</li>" +
    "<li>POST /token (pre-auth grant)</li>" +
    "<li>POST /nonce</li>" +
    "<li>POST /credential</li>" +
    "</ul><h2>Diagnostics</h2><ul>" +
    "<li><a href='/info'>/info</a></li><li><a href='/ping'>/ping</a></li>" +
    "<li><a href='/health'>/health</a></li><li><a href='/alive'>/alive</a></li>" +
    "</ul></body></html>",
    "text/html"));

app.MapOid4VciEndpoints();

app.Run();
