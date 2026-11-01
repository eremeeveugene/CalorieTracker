namespace CalorieTracker.Contracts.Catalog;

public sealed record FoodResponse(
    Guid Id,
    string Name,
    string? Brand,
    string? Barcode,
    string Source,
    NutrientsResponse Per100Grams,
    ServingResponse? DefaultServing);

// Energy in kcal, macronutrients in grams
public sealed record NutrientsResponse(decimal Calories, decimal Protein, decimal Carbohydrates, decimal Fat);

public sealed record ServingResponse(string Description, decimal Grams);
