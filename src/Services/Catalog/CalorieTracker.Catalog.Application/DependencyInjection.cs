using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Application.Barcodes;
using CalorieTracker.Catalog.Application.Foods;
using CalorieTracker.Catalog.Application.Search;
using CalorieTracker.Catalog.Domain.Foods;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CalorieTracker.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        // Catalog first, then external databases in the order the infrastructure registered them
        services.AddScoped(provider =>
        {
            var foods = provider.GetRequiredService<IFoodRepository>();
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
            var external = provider.GetServices<IBarcodeFoodSource>()
                .Select(source => new ExternalBarcodeLookup(source, foods, unitOfWork));

            return BarcodeLookup.Chain(new CatalogBarcodeLookup(foods), external);
        });

        services.AddScoped<IQueryHandler<GetFoodByBarcodeQuery, Result<Food>>, GetFoodByBarcodeQueryHandler>();
        services.AddScoped<IQueryHandler<SearchFoodsQuery, Result<IReadOnlyList<Food>>>, SearchFoodsQueryHandler>();
        services.AddScoped<IQueryHandler<GetFoodQuery, Result<Food>>, GetFoodQueryHandler>();

        return services;
    }
}
