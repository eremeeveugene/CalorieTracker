using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Domain.Foods;

namespace CalorieTracker.Catalog.Application.Search;

public sealed record SearchFoodsQuery(string Term, int Limit);

// Answers from our catalog when it has enough matches. Otherwise it asks the external source
// and keeps the new foods, so the catalog grows with what people actually search for.
internal sealed class SearchFoodsQueryHandler(
    IFoodRepository foods,
    IFoodSearchSource externalSearch,
    IUnitOfWork unitOfWork)
    : IQueryHandler<SearchFoodsQuery, Result<IReadOnlyList<Food>>>
{
    public const int MinTermLength = 2;
    public const int MaxLimit = 50;

    public async Task<Result<IReadOnlyList<Food>>> HandleAsync(SearchFoodsQuery query, CancellationToken cancellationToken)
    {
        var term = query.Term?.Trim() ?? string.Empty;
        if (term.Length < MinTermLength)
        {
            return Result.Failure<IReadOnlyList<Food>>(CatalogErrors.SearchTermTooShort);
        }

        if (query.Limit is < 1 or > MaxLimit)
        {
            return Result.Failure<IReadOnlyList<Food>>(CatalogErrors.InvalidLimit);
        }

        var local = await foods.SearchAsync(term, query.Limit, cancellationToken);
        if (local.Count >= query.Limit)
        {
            return Result.Success(local);
        }

        var external = await externalSearch.SearchAsync(term, query.Limit, cancellationToken);
        var kept = await KeepAsync(external, cancellationToken);

        IReadOnlyList<Food> results = [.. local.Concat(kept).DistinctBy(food => food.Id).Take(query.Limit)];
        return Result.Success(results);
    }

    // Swaps foods we already have for the stored copies and saves the rest
    private async Task<IReadOnlyList<Food>> KeepAsync(IReadOnlyList<Food> external, CancellationToken cancellationToken)
    {
        if (external.Count == 0)
        {
            return external;
        }

        var origins = external.Select(food => new FoodOrigin(food.Source, food.SourceId)).ToList();
        var stored = (await foods.FindByOriginsAsync(origins, cancellationToken))
            .ToDictionary(food => new FoodOrigin(food.Source, food.SourceId));

        var kept = new List<Food>(external.Count);
        foreach (var food in external.DistinctBy(food => new FoodOrigin(food.Source, food.SourceId)))
        {
            if (stored.TryGetValue(new FoodOrigin(food.Source, food.SourceId), out var existing))
            {
                kept.Add(existing);
                continue;
            }

            foods.Add(food);
            kept.Add(food);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return kept;
    }
}
