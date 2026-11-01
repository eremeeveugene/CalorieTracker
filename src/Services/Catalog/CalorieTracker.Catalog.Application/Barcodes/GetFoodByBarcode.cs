using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Catalog.Domain.Foods;

namespace CalorieTracker.Catalog.Application.Barcodes;

public sealed record GetFoodByBarcodeQuery(string Barcode);

internal sealed class GetFoodByBarcodeQueryHandler(BarcodeLookup lookup)
    : IQueryHandler<GetFoodByBarcodeQuery, Result<Food>>
{
    public async Task<Result<Food>> HandleAsync(GetFoodByBarcodeQuery query, CancellationToken cancellationToken)
    {
        var barcode = Barcode.Create(query.Barcode);
        if (barcode.IsFailure)
        {
            return Result.Failure<Food>(barcode.Error);
        }

        var food = await lookup.FindAsync(barcode.Value, cancellationToken);
        return food is null ? Result.Failure<Food>(CatalogErrors.FoodNotFound) : Result.Success(food);
    }
}
