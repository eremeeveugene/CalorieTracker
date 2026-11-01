using System.Globalization;
using System.Net.Http.Json;
using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Domain.Foods;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.Catalog.Infrastructure.Sources.Usda;

public sealed class UsdaOptions
{
    public const string SectionName = "Usda";

    public Uri? BaseAddress { get; set; }

    // Public demo key: about 10 requests per hour. Get a free key at https://fdc.nal.usda.gov/api-key-signup
    // and set it with: dotnet user-secrets set "Usda:ApiKey" "<key>" (in the Catalog API project)
    public string ApiKey { get; set; } = "DEMO_KEY";
}

// GoF Adapter: turns USDA FoodData Central foods into catalog foods. Search covers generic
// foods (Foundation, SR Legacy); barcodes are matched against US branded foods.
internal sealed class UsdaFoodDataClient(
    HttpClient httpClient,
    TimeProvider timeProvider,
    ILogger<UsdaFoodDataClient> logger)
    : IFoodSearchSource, IBarcodeFoodSource
{
    private const string SourceName = "USDA FoodData Central";
    private const string GenericFoods = "Foundation,SR Legacy";
    private const string BrandedFoods = "Branded";
    private const int BarcodeCandidates = 5;

    // USDA nutrient numbers. Some Foundation foods only report Atwater energy.
    private const string Energy = "208";
    private const string EnergyAtwaterSpecific = "958";
    private const string EnergyAtwaterGeneral = "957";
    private const string Protein = "203";
    private const string Fat = "204";
    private const string Carbohydrates = "205";

    public async Task<IReadOnlyList<Food>> SearchAsync(string term, int limit, CancellationToken cancellationToken)
    {
        var response = await SearchFoodsAsync(term, GenericFoods, limit, cancellationToken);
        return response is null ? [] : [.. response.Foods.Select(food => ToFood(food, barcode: null)).OfType<Food>()];
    }

    public async Task<Food?> FindByBarcodeAsync(Barcode barcode, CancellationToken cancellationToken)
    {
        // Search is fuzzy, so only an exact barcode match counts
        var response = await SearchFoodsAsync(barcode.PrintedForm, BrandedFoods, BarcodeCandidates, cancellationToken);
        var match = response?.Foods.FirstOrDefault(food => Barcode.Create(food.GtinUpc) is { IsSuccess: true } parsed
            && parsed.Value == barcode);

        return match is null ? null : ToFood(match, barcode);
    }

    private async Task<SearchResponse?> SearchFoodsAsync(
        string query, string dataTypes, int pageSize, CancellationToken cancellationToken)
    {
        var path = string.Create(
            CultureInfo.InvariantCulture,
            $"fdc/v1/foods/search?query={Uri.EscapeDataString(query)}&dataType={Uri.EscapeDataString(dataTypes)}&pageSize={pageSize}");

        try
        {
            return await httpClient.GetFromJsonAsync<SearchResponse>(path, SourceJson.Options, cancellationToken);
        }
        catch (Exception exception) when (SourceJson.IsUnavailable(exception, cancellationToken))
        {
            SourceLog.Unavailable(logger, SourceName, exception);
            return null;
        }
    }

    private Food? ToFood(UsdaFood usdaFood, Barcode? barcode)
    {
        var calories = Amount(usdaFood, Energy) ?? Amount(usdaFood, EnergyAtwaterSpecific) ?? Amount(usdaFood, EnergyAtwaterGeneral);
        if (calories is null
            || Amount(usdaFood, Protein) is not { } protein
            || Amount(usdaFood, Carbohydrates) is not { } carbohydrates
            || Amount(usdaFood, Fat) is not { } fat)
        {
            return null;
        }

        var nutrients = Nutrients.Create(calories.Value, protein, carbohydrates, fat);
        if (nutrients.IsFailure)
        {
            return null;
        }

        var food = Food.Create(
            usdaFood.Description ?? string.Empty,
            usdaFood.BrandName ?? usdaFood.BrandOwner,
            barcode,
            new FoodOrigin(FoodSource.Usda, usdaFood.FdcId.ToString(CultureInfo.InvariantCulture)),
            nutrients.Value,
            ToServing(usdaFood),
            timeProvider.GetUtcNow());

        return food.IsSuccess ? food.Value : null;
    }

    // Branded foods give a serving in grams plus a household measure such as "1 CONTAINER"
    private static Serving? ToServing(UsdaFood usdaFood)
    {
        if (usdaFood.ServingSize is not > 0 || !string.Equals(usdaFood.ServingSizeUnit, "g", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var grams = usdaFood.ServingSize.Value;
        var household = usdaFood.HouseholdServingFullText?.Trim();
        var description = string.IsNullOrEmpty(household)
            ? string.Create(CultureInfo.InvariantCulture, $"{grams:0.#} g")
            : string.Create(CultureInfo.InvariantCulture, $"{household} ({grams:0.#} g)");

        return description.Length <= Serving.MaxDescriptionLength ? new Serving(description, grams) : null;
    }

    private static decimal? Amount(UsdaFood usdaFood, string nutrientNumber) =>
        usdaFood.FoodNutrients?.FirstOrDefault(nutrient => nutrient.NutrientNumber == nutrientNumber)?.Value;

    private sealed record SearchResponse(IReadOnlyList<UsdaFood> Foods);

    private sealed record UsdaFood(
        long FdcId,
        string? Description,
        string? BrandOwner,
        string? BrandName,
        string? GtinUpc,
        decimal? ServingSize,
        string? ServingSizeUnit,
        string? HouseholdServingFullText,
        IReadOnlyList<UsdaNutrient>? FoodNutrients);

    private sealed record UsdaNutrient(string? NutrientNumber, decimal? Value);
}
