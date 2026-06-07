var builder = DistributedApplication.CreateBuilder(args);

// launchProfileName: "https" exposes the HTTPS endpoint from each service's
// Properties/launchSettings.json — required because YARP destinations are https://.
var identityCsp = builder.AddProject<Projects.TrustCartographer_IdentityCsp>("identity-csp", launchProfileName: "https");
var issuer      = builder.AddProject<Projects.TrustCartographer_Issuer>("issuer",     launchProfileName: "https");
var holder      = builder.AddProject<Projects.TrustCartographer_Holder>("holder",     launchProfileName: "https");
var verifier    = builder.AddProject<Projects.TrustCartographer_Verifier>("verifier", launchProfileName: "https");

builder.AddProject<Projects.TrustCartographer_Web>("web", launchProfileName: "https")
    .WithReference(identityCsp)
    .WithReference(issuer)
    .WithReference(holder)
    .WithReference(verifier)
    .WaitFor(identityCsp)
    .WaitFor(issuer)
    .WaitFor(holder)
    .WaitFor(verifier);

builder.Build().Run();
