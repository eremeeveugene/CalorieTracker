using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Domain.Foods;
using Microsoft.EntityFrameworkCore;

namespace CalorieTracker.Catalog.Infrastructure.Persistence;

internal sealed class FoodRepository(CatalogDbContext dbContext) : IFoodRepository
{
    private const string LikeEscape = "\\";

    public Task<Food?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Foods.SingleOrDefaultAsync(food => food.Id == id, cancellationToken);

    public Task<Food?> FindByBarcodeAsync(Barcode barcode, CancellationToken cancellationToken) =>
        dbContext.Foods.SingleOrDefaultAsync(food => food.Barcode == barcode, cancellationToken);

    public async Task<IReadOnlyList<Food>> FindByOriginsAsync(
        IReadOnlyCollection<FoodOrigin> origins, CancellationToken cancellationToken)
    {
        var found = new List<Food>();
        foreach (var group in origins.GroupBy(origin => origin.Source))
        {
            var sourceIds = group.Select(origin => origin.SourceId).ToList();
            found.AddRange(await dbContext.Foods
                .Where(food => food.Source == group.Key && sourceIds.Contains(food.SourceId))
                .ToListAsync(cancellationToken));
        }

        return found;
    }

    // Matches anywhere in the name or brand; shorter names first, so "Bananas, raw" beats
    // "Bananas, dehydrated, or banana powder" for "banana"
    public async Task<IReadOnlyList<Food>> SearchAsync(string term, int limit, CancellationToken cancellationToken)
    {
        var pattern = $"%{EscapeLike(term)}%";

        return await dbContext.Foods
            .Where(food => EF.Functions.ILike(food.Name, pattern, LikeEscape)
                || (food.Brand != null && EF.Functions.ILike(food.Brand, pattern, LikeEscape)))
            .OrderBy(food => food.Name.Length)
            .ThenBy(food => food.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public void Add(Food food) => dbContext.Foods.Add(food);

    // "%" and "_" typed by the user are literal characters, not wildcards
    private static string EscapeLike(string term) =>
        term.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
}
