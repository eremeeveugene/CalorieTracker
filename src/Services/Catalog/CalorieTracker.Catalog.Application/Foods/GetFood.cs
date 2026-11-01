using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Domain.Foods;

namespace CalorieTracker.Catalog.Application.Foods;

public sealed record GetFoodQuery(Guid Id);

internal sealed class GetFoodQueryHandler(IFoodRepository foods) : IQueryHandler<GetFoodQuery, Result<Food>>
{
    public async Task<Result<Food>> HandleAsync(GetFoodQuery query, CancellationToken cancellationToken)
    {
        var food = await foods.FindByIdAsync(query.Id, cancellationToken);
        return food is null ? Result.Failure<Food>(CatalogErrors.FoodNotFound) : Result.Success(food);
    }
}
