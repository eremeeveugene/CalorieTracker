using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CalorieTracker.Catalog.Application.Abstractions;
using CalorieTracker.Catalog.Domain.Foods;
using Microsoft.Extensions.Logging;

namespace CalorieTracker.Catalog.Infrastructure.Sources.OpenFoodFacts;

public sealed class OpenFoodFactsOptions
{
    public const string SectionName = "OpenFoodFacts";

    public Uri? BaseAddress { get; set; }

    // Open Food Facts asks every app to identify itself
    public string UserAgent { get; set; } = "CalorieTracker";
}

// GoF Adapter: turns Open Food Facts products (worldwide, packaged foods) into catalog foods
internal sealed class OpenFoodFactsClient(
    HttpClient httpClient,
    TimeProvider timeProvider,
    ILogger<OpenFoodFactsClient> logger)
    : IBarcodeFoodSource
{
    private const string SourceName = "Open Food Facts";
    private const string Fields = "code,product_name,brands,nutriments,serving_size,serving_quantity";
    private const int ProductFound = 1;
    private const decimal KilojoulesPerKilocalorie = 4.184m;

    public async Task<Food?> FindByBarcodeAsync(Barcode barcode, CancellationToken cancellationToken)
    {
        try
        {
            // Unknown barcodes still answer 200, with status 0
            var response = await httpClient.GetFromJsonAsync<ProductResponse>(
                $"api/v2/product/{barcode.PrintedForm}?fields={Fields}", SourceJson.Options, cancellationToken);

            return response is { Status: ProductFound, Product: { } product } ? ToFood(barcode, product) : null;
        }
        catch (Exception exception) when (SourceJson.IsUnavailable(exception, cancellationToken))
        {
            SourceLog.Unavailable(logger, SourceName, exception);
            return null;
        }
    }

    // Products with missing or impossible nutrition data are skipped rather than guessed
    private Food? ToFood(Barcode barcode, Product product)
    {
        var nutriments = product.Nutriments;
        var calories = nutriments?.EnergyKcal ?? (nutriments?.EnergyKj ?? nutriments?.Energy) / KilojoulesPerKilocalorie;
        if (string.IsNullOrWhiteSpace(product.ProductName)
            || calories is null
            || nutriments?.Proteins is not { } protein
            || nutriments.Carbohydrates is not { } carbohydrates
            || nutriments.Fat is not { } fat)
        {
            return null;
        }

        var nutrients = Nutrients.Create(calories.Value, protein, carbohydrates, fat);
        if (nutrients.IsFailure)
        {
            return null;
        }

        var food = Food.Create(
            product.ProductName,
            FirstBrand(product.Brands),
            barcode,
            new FoodOrigin(FoodSource.OpenFoodFacts, barcode.PrintedForm),
            nutrients.Value,
            ToServing(product),
            timeProvider.GetUtcNow());

        return food.IsSuccess ? food.Value : null;
    }

    private static Serving? ToServing(Product product)
    {
        if (product.ServingQuantity is not > 0)
        {
            return null;
        }

        var grams = product.ServingQuantity.Value;
        var description = product.ServingSize?.Trim();
        return description is { Length: > 0 and <= Serving.MaxDescriptionLength }
            ? new Serving(description, grams)
            : new Serving($"{grams:0.#} g", grams);
    }

    private static string? FirstBrand(string? brands) =>
        brands?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

    private sealed record ProductResponse(
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("product")] Product? Product);

    private sealed record Product(
        [property: JsonPropertyName("product_name")] string? ProductName,
        [property: JsonPropertyName("brands")] string? Brands,
        [property: JsonPropertyName("serving_size")] string? ServingSize,
        [property: JsonPropertyName("serving_quantity")] decimal? ServingQuantity,
        [property: JsonPropertyName("nutriments")] Nutriments? Nutriments);

    private sealed record Nutriments(
        [property: JsonPropertyName("energy-kcal_100g")] decimal? EnergyKcal,
        [property: JsonPropertyName("energy-kj_100g")] decimal? EnergyKj,
        [property: JsonPropertyName("energy_100g")] decimal? Energy,
        [property: JsonPropertyName("proteins_100g")] decimal? Proteins,
        [property: JsonPropertyName("carbohydrates_100g")] decimal? Carbohydrates,
        [property: JsonPropertyName("fat_100g")] decimal? Fat);
}
