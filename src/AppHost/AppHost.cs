var builder = DistributedApplication.CreateBuilder(args);

// Persistent container + volume: data and a fast start survive AppHost restarts
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var identityDb = postgres.AddDatabase("identitydb");

var identity = builder.AddProject<Projects.CalorieTracker_Identity_Api>("identity")
    .WithReference(identityDb)
    .WaitFor(identityDb);

// The only entry point clients can reach; services stay on the internal network
builder.AddProject<Projects.CalorieTracker_Gateway>("gateway")
    .WithReference(identity)
    .WaitFor(identity)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
