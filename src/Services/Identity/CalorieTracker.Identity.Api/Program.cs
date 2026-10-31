using CalorieTracker.Identity.Api.Endpoints;
using CalorieTracker.Identity.Application;
using CalorieTracker.Identity.Infrastructure;
using CalorieTracker.Identity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddIdentityInfrastructure();
builder.Services.AddIdentityApplication();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Production applies migrations from a deployment job instead
    await app.Services.ApplyIdentityMigrationsAsync();
}

app.MapDefaultEndpoints();
app.MapAuthEndpoints();

await app.RunAsync();
