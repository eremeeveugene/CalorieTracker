var builder = DistributedApplication.CreateBuilder(args);

// Persistent container + volume: data and a fast start survive AppHost restarts
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var identityDb = postgres.AddDatabase("identitydb");

builder.AddProject<Projects.CalorieTracker_Identity_Api>("identity")
    .WithReference(identityDb)
    .WaitFor(identityDb);

await builder.Build().RunAsync();
