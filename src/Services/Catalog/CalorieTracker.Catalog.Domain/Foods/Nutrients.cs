using CalorieTracker.BuildingBlocks.Results;

namespace CalorieTracker.Catalog.Domain.Foods;

// Energy (kcal) and macronutrients (g) per 100 g of food
public sealed record Nutrients
{
    // Pure fat has about 900 kcal per 100 g; the tolerances absorb rounding on food labels
    private const decimal MaxCalories = 900m;
    private const decimal CaloriesTolerance = 10m;
    private const decimal MaxMacroGrams = 100m;
    private const decimal MacroTolerance = 1m;
    private const int Decimals = 1;

    private Nutrients(decimal calories, decimal protein, decimal carbohydrates, decimal fat)
    {
        Calories = calories;
        Protein = protein;
        Carbohydrates = carbohydrates;
        Fat = fat;
    }

    public decimal Calories { get; private init; }

    public decimal Protein { get; private init; }

    public decimal Carbohydrates { get; private init; }

    public decimal Fat { get; private init; }

    public static Result<Nutrients> Create(decimal calories, decimal protein, decimal carbohydrates, decimal fat)
    {
        var isNegative = calories < 0 || protein < 0 || carbohydrates < 0 || fat < 0;
        var isImpossible = calories > MaxCalories + CaloriesTolerance
            || protein + carbohydrates + fat > MaxMacroGrams + MacroTolerance;

        if (isNegative || isImpossible)
        {
            return Result.Failure<Nutrients>(FoodErrors.InvalidNutrients);
        }

        return Result.Success(new Nutrients(
            Math.Round(calories, Decimals),
            Math.Round(protein, Decimals),
            Math.Round(carbohydrates, Decimals),
            Math.Round(fat, Decimals)));
    }
}
