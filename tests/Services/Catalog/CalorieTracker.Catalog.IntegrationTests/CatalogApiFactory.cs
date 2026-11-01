using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Domain.Foods;
using CalorieTracker.Catalog.Infrastructure;
using CalorieTracker.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace CalorieTracker.Catalog.IntegrationTests;

// The real Catalog API on a throwaway Postgres. External food databases are replaced with
// fakes, so tests never depend on Open Food Facts or USDA being up (or on their rate limits).
public sealed class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public FakeIdentityServer Identity { get; private set; } = null!;

    public FakeFoodSource Sources { get; } = new();

    public async Task InitializeAsync()
    {
        Identity = await FakeIdentityServer.StartAsync();
        await _postgres.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
        await Identity.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.DatabaseName}", _postgres.GetConnectionString());
        builder.UseSetting(
            $"{AuthenticationExtensions.ConfigurationSection}:MetadataAddress",
            new Uri(Identity.BaseAddress, ".well-known/openid-configuration").ToString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBarcodeFoodSource>();
            services.RemoveAll<IFoodSearchSource>();
            services.AddSingleton<IBarcodeFoodSource>(Sources);
            services.AddSingleton<IFoodSearchSource>(Sources);
        });
    }
}

public sealed class FakeFoodSource : IBarcodeFoodSource, IFoodSearchSource
{
    public const string Nutella = "3017624010701";

    private int _barcodeCalls;
    private int _searchCalls;

    public int BarcodeCalls => _barcodeCalls;

    public int SearchCalls => _searchCalls;

    public Task<Food?> FindByBarcodeAsync(Barcode barcode, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _barcodeCalls);
        var food = barcode.PrintedForm == Nutella
            ? Create("Nutella", "Ferrero", barcode, FoodSource.OpenFoodFacts, Nutella, Nutrients.Create(539m, 6.3m, 57.5m, 30.9m).Value)
            : null;

        return Task.FromResult(food);
    }

    // Two generic foods named after the search term, always with the same USDA ids
    public Task<IReadOnlyList<Food>> SearchAsync(string term, int limit, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _searchCalls);
        var nutrients = Nutrients.Create(89m, 1.1m, 22.8m, 0.3m).Value;

        IReadOnlyList<Food> foods =
        [
            Create($"{term}, raw", null, null, FoodSource.Usda, $"{term}-raw", nutrients),
            Create($"{term}, dried", null, null, FoodSource.Usda, $"{term}-dried", nutrients),
        ];
        return Task.FromResult(foods);
    }

    private static Food Create(string name, string? brand, Barcode? barcode, FoodSource source, string sourceId, Nutrients nutrients) =>
        Food.Create(name, brand, barcode, new FoodOrigin(source, sourceId), nutrients, new Serving("1 portion", 100m), DateTimeOffset.UtcNow).Value;
}
