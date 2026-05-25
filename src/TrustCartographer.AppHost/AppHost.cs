var builder = DistributedApplication.CreateBuilder(args);

// launchProfileName: "https" exposes the HTTPS endpoint from each service's
// Properties/launchSettings.json — required because YARP destinations are https://.
var issuer   = builder.AddProject<Projects.TrustCartographer_Issuer>("issuer",     launchProfileName: "https");
var holder   = builder.AddProject<Projects.TrustCartographer_Holder>("holder",     launchProfileName: "https");
var verifier = builder.AddProject<Projects.TrustCartographer_Verifier>("verifier", launchProfileName: "https");

builder.AddProject<Projects.TrustCartographer_Web>("web", launchProfileName: "https")
    .WithReference(issuer)
    .WithReference(holder)
    .WithReference(verifier)
    .WaitFor(issuer)
    .WaitFor(holder)
    .WaitFor(verifier);

builder.Build().Run();
