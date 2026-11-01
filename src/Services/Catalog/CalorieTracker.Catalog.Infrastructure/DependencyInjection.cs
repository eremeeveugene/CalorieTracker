using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Infrastructure.Persistence;
using CalorieTracker.Catalog.Infrastructure.Sources.OpenFoodFacts;
using CalorieTracker.Catalog.Infrastructure.Sources.Usda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CalorieTracker.Catalog.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseName = "catalogdb";

    private const string UsdaApiKeyHeader = "X-Api-Key";

    public static IHostApplicationBuilder AddCatalogInfrastructure(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddNpgsqlDbContext<CatalogDbContext>(DatabaseName);
        builder.Services.AddScoped<IFoodRepository, FoodRepository>();
        builder.Services.AddScoped<IUnitOfWork>(services => services.GetRequiredService<CatalogDbContext>());

        builder.Services.AddOptions<OpenFoodFactsOptions>()
            .BindConfiguration(OpenFoodFactsOptions.SectionName)
            .Validate(options => options.BaseAddress is not null, "OpenFoodFacts:BaseAddress is required.")
            .ValidateOnStart();
        builder.Services.AddOptions<UsdaOptions>()
            .BindConfiguration(UsdaOptions.SectionName)
            .Validate(options => options.BaseAddress is not null, "Usda:BaseAddress is required.")
            .ValidateOnStart();

        builder.Services.AddHttpClient<OpenFoodFactsClient>((services, client) =>
        {
            var options = services.GetRequiredService<IOptions<OpenFoodFactsOptions>>().Value;
            client.BaseAddress = options.BaseAddress;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        });

        // The key travels in a header rather than the URL, so it never shows up in request logs
        builder.Services.AddHttpClient<UsdaFoodDataClient>((services, client) =>
        {
            var options = services.GetRequiredService<IOptions<UsdaOptions>>().Value;
            client.BaseAddress = options.BaseAddress;
            client.DefaultRequestHeaders.Add(UsdaApiKeyHeader, options.ApiKey);
        });

        // Barcode lookups ask Open Food Facts (worldwide) before USDA (US branded foods)
        builder.Services.AddScoped<IBarcodeFoodSource>(services => services.GetRequiredService<OpenFoodFactsClient>());
        builder.Services.AddScoped<IBarcodeFoodSource>(services => services.GetRequiredService<UsdaFoodDataClient>());
        builder.Services.AddScoped<IFoodSearchSource>(services => services.GetRequiredService<UsdaFoodDataClient>());

        return builder;
    }
}
