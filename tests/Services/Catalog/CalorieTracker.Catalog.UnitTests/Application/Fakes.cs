using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Domain.Foods;

namespace CalorieTracker.Catalog.UnitTests.Application;

internal static class TestFoods
{
    public static Food Create(string name, FoodSource source, string sourceId, string? barcode = null) =>
        Food.Create(
            name,
            brand: null,
            barcode is null ? null : Barcode.Create(barcode).Value,
            new FoodOrigin(source, sourceId),
            Nutrients.Create(100m, 1m, 20m, 1m).Value,
            defaultServing: null,
            DateTimeOffset.UtcNow).Value;
}

internal sealed class InMemoryFoodRepository : IFoodRepository, IUnitOfWork
{
    private readonly List<Food> _pending = [];

    public List<Food> Saved { get; } = [];

    public Task<Food?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Saved.SingleOrDefault(food => food.Id == id));

    public Task<Food?> FindByBarcodeAsync(Barcode barcode, CancellationToken cancellationToken) =>
        Task.FromResult(Saved.SingleOrDefault(food => food.Barcode == barcode));

    public Task<IReadOnlyList<Food>> FindByOriginsAsync(IReadOnlyCollection<FoodOrigin> origins, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Food>>(
            [.. Saved.Where(food => origins.Contains(new FoodOrigin(food.Source, food.SourceId)))]);

    public Task<IReadOnlyList<Food>> SearchAsync(string term, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Food>>(
            [.. Saved.Where(food => food.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).Take(limit)]);

    public void Add(Food food) => _pending.Add(food);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saved.AddRange(_pending);
        _pending.Clear();
        return Task.CompletedTask;
    }
}

internal sealed class FakeBarcodeSource(params Food[] foods) : IBarcodeFoodSource
{
    public int Calls { get; private set; }

    public Task<Food?> FindByBarcodeAsync(Barcode barcode, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(foods.FirstOrDefault(food => food.Barcode == barcode));
    }
}

internal sealed class FakeSearchSource(Func<string, IReadOnlyList<Food>> search) : IFoodSearchSource
{
    public int Calls { get; private set; }

    public Task<IReadOnlyList<Food>> SearchAsync(string term, int limit, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(search(term));
    }
}
