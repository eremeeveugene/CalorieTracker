using CalorieTracker.Catalog.Api.Endpoints;
using CalorieTracker.Catalog.Application;
using CalorieTracker.Catalog.Infrastructure;
using CalorieTracker.Catalog.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddJwtAuthentication();
builder.AddCatalogInfrastructure();
builder.Services.AddCatalogApplication();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    // Production applies migrations from a deployment job instead
    await app.Services.ApplyCatalogMigrationsAsync();
}

app.MapDefaultEndpoints();
app.MapFoodEndpoints();

await app.RunAsync();
