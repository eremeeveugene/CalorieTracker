using CalorieTracker.BuildingBlocks.Results;

namespace CalorieTracker.Catalog.Domain.Foods;

public enum FoodSource
{
    OpenFoodFacts,
    Usda,
}

// Where a food came from; the pair is unique, so the same product is never stored twice
public sealed record FoodOrigin(FoodSource Source, string SourceId);

public sealed record Serving(string Description, decimal Grams)
{
    public const int MaxDescriptionLength = 100;
}

public sealed class Food
{
    public const int MaxNameLength = 200;

    // For EF Core
    private Food()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Brand { get; private set; }

    public Barcode? Barcode { get; private set; }

    public FoodSource Source { get; private set; }

    public string SourceId { get; private set; } = string.Empty;

    public Nutrients Per100Grams { get; private set; } = null!;

    public string? ServingDescription { get; private set; }

    public decimal? ServingGrams { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Serving? DefaultServing =>
        ServingDescription is not null && ServingGrams is { } grams ? new Serving(ServingDescription, grams) : null;

    public static Result<Food> Create(
        string name,
        string? brand,
        Barcode? barcode,
        FoodOrigin origin,
        Nutrients per100Grams,
        Serving? defaultServing,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(per100Grams);

        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0 || trimmedName.Length > MaxNameLength)
        {
            return Result.Failure<Food>(FoodErrors.InvalidName);
        }

        if (defaultServing is not null && !IsValid(defaultServing))
        {
            return Result.Failure<Food>(FoodErrors.InvalidServing);
        }

        return Result.Success(new Food
        {
            Id = Guid.CreateVersion7(),
            Name = trimmedName,
            Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim(),
            Barcode = barcode,
            Source = origin.Source,
            SourceId = origin.SourceId,
            Per100Grams = per100Grams,
            ServingDescription = defaultServing?.Description.Trim(),
            ServingGrams = defaultServing?.Grams,
            CreatedAt = now,
        });
    }

    private static bool IsValid(Serving serving) =>
        !string.IsNullOrWhiteSpace(serving.Description)
        && serving.Description.Trim().Length <= Serving.MaxDescriptionLength
        && serving.Grams > 0;
}
