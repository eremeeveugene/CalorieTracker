using CalorieTracker.Catalog.Domain.Foods;

namespace CalorieTracker.Catalog.Application.Abstractions;

public interface IFoodRepository
{
    Task<Food?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Food?> FindByBarcodeAsync(Barcode barcode, CancellationToken cancellationToken);

    Task<IReadOnlyList<Food>> FindByOriginsAsync(IReadOnlyCollection<FoodOrigin> origins, CancellationToken cancellationToken);

    Task<IReadOnlyList<Food>> SearchAsync(string term, int limit, CancellationToken cancellationToken);

    void Add(Food food);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

// External food databases. The infrastructure adapts each API to these ports (GoF Adapter).
// Sources return null or nothing when they are unreachable, so the app degrades instead of failing.
public interface IBarcodeFoodSource
{
    Task<Food?> FindByBarcodeAsync(Barcode barcode, CancellationToken cancellationToken);
}

public interface IFoodSearchSource
{
    Task<IReadOnlyList<Food>> SearchAsync(string term, int limit, CancellationToken cancellationToken);
}
