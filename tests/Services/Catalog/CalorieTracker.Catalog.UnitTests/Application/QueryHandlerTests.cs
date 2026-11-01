using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Catalog.Application;
using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Application.Barcodes;
using CalorieTracker.Catalog.Application.Search;
using CalorieTracker.Catalog.Domain.Foods;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker.Catalog.UnitTests.Application;

// Resolves handlers through AddCatalogApplication, so the chain is wired exactly as in production
public sealed class QueryHandlerTests : IDisposable
{
    private const string Nutella = "3017624010701";

    private readonly InMemoryFoodRepository _repository = new();
    private readonly FakeBarcodeSource _openFoodFacts = new(TestFoods.Create("Nutella", FoodSource.OpenFoodFacts, Nutella, Nutella));
    private readonly FakeBarcodeSource _usda = new(TestFoods.Create("NUTELLA WITH BREADSTICKS", FoodSource.Usda, "2026852", "009800800124"));
    private readonly FakeSearchSource _search = new(term => [TestFoods.Create($"{term}, raw", FoodSource.Usda, $"{term}-1")]);
    private readonly ServiceProvider _services;
    private readonly IServiceScope _scope;

    public QueryHandlerTests()
    {
        _services = new ServiceCollection()
            .AddCatalogApplication()
            .AddSingleton<IFoodRepository>(_repository)
            .AddSingleton<IUnitOfWork>(_repository)
            .AddSingleton<IBarcodeFoodSource>(_openFoodFacts)
            .AddSingleton<IBarcodeFoodSource>(_usda)
            .AddSingleton<IFoodSearchSource>(_search)
            .BuildServiceProvider();
        _scope = _services.CreateScope();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _services.Dispose();
    }

    [Fact]
    public async Task Barcode_InCatalog_DoesNotAskExternalSources()
    {
        _repository.Saved.Add(TestFoods.Create("Nutella", FoodSource.OpenFoodFacts, Nutella, Nutella));

        var result = await LookUpAsync(Nutella);

        result.Value.Name.ShouldBe("Nutella");
        _openFoodFacts.Calls.ShouldBe(0);
        _usda.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Barcode_OnlyInUsda_TriesOpenFoodFactsFirstAndKeepsTheFood()
    {
        var first = await LookUpAsync("009800800124");
        var second = await LookUpAsync("0009800800124");

        first.Value.Source.ShouldBe(FoodSource.Usda);
        second.Value.Id.ShouldBe(first.Value.Id);
        _openFoodFacts.Calls.ShouldBe(1);
        _usda.Calls.ShouldBe(1);
        _repository.Saved.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Barcode_UnknownEverywhere_IsNotFound()
    {
        var result = await LookUpAsync("96385074");

        result.Error.ShouldBe(CatalogErrors.FoodNotFound);
    }

    [Fact]
    public async Task Barcode_Invalid_FailsWithoutAskingAnyone()
    {
        var result = await LookUpAsync("12345");

        result.Error.ShouldBe(FoodErrors.InvalidBarcode);
        _openFoodFacts.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Search_TooShortTerm_Fails()
    {
        var result = await SearchAsync("b", limit: 20);

        result.Error.ShouldBe(CatalogErrors.SearchTermTooShort);
    }

    [Fact]
    public async Task Search_CatalogHasEnough_DoesNotAskExternalSource()
    {
        _repository.Saved.Add(TestFoods.Create("Bananas, raw", FoodSource.Usda, "173944"));

        var result = await SearchAsync("banana", limit: 1);

        result.Value.Single().Name.ShouldBe("Bananas, raw");
        _search.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Search_CatalogHasTooFew_KeepsExternalResultsWithoutDuplicates()
    {
        var first = await SearchAsync("kiwi", limit: 20);
        var second = await SearchAsync("kiwi", limit: 20);

        first.Value.Single().Name.ShouldBe("kiwi, raw");
        second.Value.Single().Id.ShouldBe(first.Value.Single().Id);
        _repository.Saved.Count.ShouldBe(1);
    }

    private Task<Result<Food>> LookUpAsync(string barcode) =>
        _scope.ServiceProvider.GetRequiredService<IQueryHandler<GetFoodByBarcodeQuery, Result<Food>>>()
            .HandleAsync(new GetFoodByBarcodeQuery(barcode), CancellationToken.None);

    private Task<Result<IReadOnlyList<Food>>> SearchAsync(string term, int limit) =>
        _scope.ServiceProvider.GetRequiredService<IQueryHandler<SearchFoodsQuery, Result<IReadOnlyList<Food>>>>()
            .HandleAsync(new SearchFoodsQuery(term, limit), CancellationToken.None);
}
