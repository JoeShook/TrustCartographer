var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/info", () => Results.Ok(new
{
    name = "verifier",
    purpose = "Requests + verifies VPs via OID4VP",
    phase = "stub"
}));

app.MapGet("/ping", () => "pong");

app.MapGet("/", () => Results.Content(
    "<html><body style='font-family:sans-serif'><h1>TrustCartographer Verifier (stub)</h1>" +
    "<ul><li><a href='/info'>/info</a></li><li><a href='/ping'>/ping</a></li>" +
    "<li><a href='/health'>/health</a></li><li><a href='/alive'>/alive</a></li></ul></body></html>",
    "text/html"));

app.Run();
