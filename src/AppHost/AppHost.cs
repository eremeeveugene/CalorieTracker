var builder = DistributedApplication.CreateBuilder(args);

// Persistent container + volume: data and a fast start survive AppHost restarts
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var identityDb = postgres.AddDatabase("identitydb");
var catalogDb = postgres.AddDatabase("catalogdb");

var identity = builder.AddProject<Projects.CalorieTracker_Identity_Api>("identity")
    .WithReference(identityDb)
    .WaitFor(identityDb);

// References identity to fetch its public signing keys and validate tokens itself
var catalog = builder.AddProject<Projects.CalorieTracker_Catalog_Api>("catalog")
    .WithReference(catalogDb)
    .WithReference(identity)
    .WaitFor(catalogDb);

// The only entry point clients can reach; services stay on the internal network
var gateway = builder.AddProject<Projects.CalorieTracker_Gateway>("gateway")
    .WithReference(identity)
    .WithReference(catalog)
    .WaitFor(identity)
    .WaitFor(catalog)
    .WithExternalHttpEndpoints();

// Development convenience: opens the desktop client already pointed at the gateway
builder.AddProject<Projects.CalorieTracker_Client_Desktop>("client-desktop")
    .WithReference(gateway)
    .WaitFor(gateway)
    .ExcludeFromManifest();

await builder.Build().RunAsync();
