using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Domain.Foods;

namespace CalorieTracker.Catalog.Application.Barcodes;

// GoF Chain of Responsibility: each link tries to find the food and otherwise passes the
// barcode on. The order is cheapest first: our catalog, then each external database.
internal abstract class BarcodeLookup
{
    private BarcodeLookup? _next;

    public static BarcodeLookup Chain(BarcodeLookup first, IEnumerable<BarcodeLookup> rest)
    {
        var last = first;
        foreach (var link in rest)
        {
            last._next = link;
            last = link;
        }

        return first;
    }

    public async Task<Food?> FindAsync(Barcode barcode, CancellationToken cancellationToken)
    {
        var food = await TryFindAsync(barcode, cancellationToken);
        if (food is not null || _next is null)
        {
            return food;
        }

        return await _next.FindAsync(barcode, cancellationToken);
    }

    protected abstract Task<Food?> TryFindAsync(Barcode barcode, CancellationToken cancellationToken);
}

internal sealed class CatalogBarcodeLookup(IFoodRepository foods) : BarcodeLookup
{
    protected override Task<Food?> TryFindAsync(Barcode barcode, CancellationToken cancellationToken) =>
        foods.FindByBarcodeAsync(barcode, cancellationToken);
}

internal sealed class ExternalBarcodeLookup(IBarcodeFoodSource source, IFoodRepository foods, IUnitOfWork unitOfWork)
    : BarcodeLookup
{
    protected override async Task<Food?> TryFindAsync(Barcode barcode, CancellationToken cancellationToken)
    {
        var food = await source.FindByBarcodeAsync(barcode, cancellationToken);
        if (food is null)
        {
            return null;
        }

        // Keep it, so the next scan of this product is answered from our catalog
        foods.Add(food);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return food;
    }
}
