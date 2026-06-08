var builder = DistributedApplication.CreateBuilder(args);

// launchProfileName: "https" exposes the HTTPS endpoint from each service's
// Properties/launchSettings.json — required because YARP destinations are https://.
var identityCsp     = builder.AddProject<Projects.TrustCartographer_IdentityCsp>("identity-csp", launchProfileName: "https");
var payerIssuer     = builder.AddProject<Projects.TrustCartographer_PayerIssuer>("payer-issuer", launchProfileName: "https");
var providerIssuer  = builder.AddProject<Projects.TrustCartographer_ProviderIssuer>("provider-issuer", launchProfileName: "https");
var cspIssuer       = builder.AddProject<Projects.TrustCartographer_CspIssuer>("csp-issuer", launchProfileName: "https")
    .WithReference(identityCsp)   // Pathway C consults the Identity-CSP for delegated-proofing assurance
    .WaitFor(identityCsp);
var issuer          = builder.AddProject<Projects.TrustCartographer_Issuer>("issuer", launchProfileName: "https");
var holder          = builder.AddProject<Projects.TrustCartographer_Holder>("holder", launchProfileName: "https");
var holderMinor     = builder.AddProject<Projects.TrustCartographer_HolderMinor>("holder-minor", launchProfileName: "https");
var verifier        = builder.AddProject<Projects.TrustCartographer_Verifier>("verifier", launchProfileName: "https");

builder.AddProject<Projects.TrustCartographer_Web>("web", launchProfileName: "https")
    .WithReference(identityCsp)
    .WithReference(payerIssuer)
    .WithReference(providerIssuer)
    .WithReference(cspIssuer)
    .WithReference(issuer)
    .WithReference(holder)
    .WithReference(holderMinor)
    .WithReference(verifier)
    .WaitFor(identityCsp)
    .WaitFor(payerIssuer)
    .WaitFor(providerIssuer)
    .WaitFor(cspIssuer)
    .WaitFor(issuer)
    .WaitFor(holder)
    .WaitFor(holderMinor)
    .WaitFor(verifier);

builder.Build().Run();
