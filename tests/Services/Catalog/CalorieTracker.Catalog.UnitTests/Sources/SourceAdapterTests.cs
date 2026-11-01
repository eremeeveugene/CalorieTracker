using System.Net;
using System.Text;
using CalorieTracker.Catalog.Domain.Foods;
using CalorieTracker.Catalog.Infrastructure.Sources.OpenFoodFacts;
using CalorieTracker.Catalog.Infrastructure.Sources.Usda;
using Microsoft.Extensions.Logging.Abstractions;

namespace CalorieTracker.Catalog.UnitTests.Sources;

// Fixtures follow the real API responses (trimmed), captured from both services
public class OpenFoodFactsClientTests
{
    private static readonly Barcode Nutella = Barcode.Create("3017624010701").Value;

    [Fact]
    public async Task FindByBarcodeAsync_FoundProduct_MapsNutritionPer100Grams()
    {
        const string json = """
            {"code":"3017624010701","product":{"brands":"Ferrero, Nutella","nutriments":{"carbohydrates_100g":57.5,
            "energy-kcal_100g":539,"energy_100g":2228,"fat_100g":30.9,"proteins_100g":6.3},
            "product_name":"Nutella","serving_quantity":"15","serving_size":"15 g"},"status":1,"status_verbose":"product found"}
            """;

        var food = await FindAsync(json);

        food.ShouldNotBeNull();
        food.Name.ShouldBe("Nutella");
        food.Brand.ShouldBe("Ferrero");
        food.Source.ShouldBe(FoodSource.OpenFoodFacts);
        food.Per100Grams.ShouldBe(Nutrients.Create(539m, 6.3m, 57.5m, 30.9m).Value);
        food.DefaultServing.ShouldBe(new Serving("15 g", 15m));
    }

    [Fact]
    public async Task FindByBarcodeAsync_OnlyKilojoules_ConvertsToKilocalories()
    {
        const string json = """
            {"product":{"product_name":"Nutella","nutriments":{"energy_100g":2228,"proteins_100g":6.3,
            "carbohydrates_100g":57.5,"fat_100g":30.9}},"status":1}
            """;

        var food = await FindAsync(json);

        food.ShouldNotBeNull();
        food.Per100Grams.Calories.ShouldBe(532.5m);
    }

    [Theory]
    [InlineData("""{"code":"00000000","status":0,"status_verbose":"no code or invalid code"}""")]
    [InlineData("""{"product":{"product_name":"Mystery jar","nutriments":{"energy-kcal_100g":539}},"status":1}""")]
    public async Task FindByBarcodeAsync_UnknownOrIncompleteProduct_ReturnsNull(string json)
    {
        (await FindAsync(json)).ShouldBeNull();
    }

    [Fact]
    public async Task FindByBarcodeAsync_ServiceDown_ReturnsNull()
    {
        using var httpClient = StubHttp.Throwing(new HttpRequestException("Connection refused"));
        var client = new OpenFoodFactsClient(httpClient, TimeProvider.System, NullLogger<OpenFoodFactsClient>.Instance);

        (await client.FindByBarcodeAsync(Nutella, CancellationToken.None)).ShouldBeNull();
    }

    private static async Task<Food?> FindAsync(string json)
    {
        using var httpClient = StubHttp.Returning(json);
        var client = new OpenFoodFactsClient(httpClient, TimeProvider.System, NullLogger<OpenFoodFactsClient>.Instance);
        return await client.FindByBarcodeAsync(Nutella, CancellationToken.None);
    }
}

public class UsdaFoodDataClientTests
{
    private const string GenericSearch = """
        {"totalHits":61,"foods":[
          {"fdcId":173944,"description":"Bananas, raw","dataType":"SR Legacy","foodNutrients":[
            {"nutrientNumber":"268","unitName":"kJ","value":371},{"nutrientNumber":"205","unitName":"G","value":22.8},
            {"nutrientNumber":"208","unitName":"KCAL","value":89.0},{"nutrientNumber":"203","unitName":"G","value":1.09},
            {"nutrientNumber":"204","unitName":"G","value":0.33}]},
          {"fdcId":2344720,"description":"Bananas, ripe, raw","dataType":"Foundation","foodNutrients":[
            {"nutrientNumber":"958","unitName":"KCAL","value":98.0},{"nutrientNumber":"203","unitName":"G","value":0.74},
            {"nutrientNumber":"204","unitName":"G","value":0.29},{"nutrientNumber":"205","unitName":"G","value":23.0}]},
          {"fdcId":1,"description":"Banana flavour, no nutrients","dataType":"SR Legacy","foodNutrients":[]}]}
        """;

    private const string BrandedSearch = """
        {"totalHits":2,"foods":[
          {"fdcId":999,"description":"SOMETHING ELSE","gtinUpc":"009800800117","foodNutrients":[
            {"nutrientNumber":"208","value":100},{"nutrientNumber":"203","value":1},{"nutrientNumber":"204","value":1},{"nutrientNumber":"205","value":1}]},
          {"fdcId":2026852,"description":"NUTELLA FERRERO WITH BREADSTICKS","brandOwner":"Ferrero U.S.A., Incorporated",
           "brandName":"NUTELLA FERRERO","gtinUpc":"009800800124","servingSize":52.0,"servingSizeUnit":"g",
           "householdServingFullText":"1 CONTAINER","foodNutrients":[
            {"nutrientNumber":"203","value":7.69},{"nutrientNumber":"204","value":26.9},
            {"nutrientNumber":"205","value":65.4},{"nutrientNumber":"208","value":519}]}]}
        """;

    [Fact]
    public async Task SearchAsync_MapsGenericFoodsAndSkipsIncompleteOnes()
    {
        using var httpClient = StubHttp.Returning(GenericSearch);
        var client = new UsdaFoodDataClient(httpClient, TimeProvider.System, NullLogger<UsdaFoodDataClient>.Instance);

        var foods = await client.SearchAsync("banana", 20, CancellationToken.None);

        foods.Select(food => food.Name).ShouldBe(["Bananas, raw", "Bananas, ripe, raw"]);
        foods[0].Per100Grams.ShouldBe(Nutrients.Create(89m, 1.1m, 22.8m, 0.3m).Value);
        foods[0].SourceId.ShouldBe("173944");
        foods[1].Per100Grams.Calories.ShouldBe(98m);
    }

    [Fact]
    public async Task FindByBarcodeAsync_PicksTheExactBarcodeMatch()
    {
        using var httpClient = StubHttp.Returning(BrandedSearch);
        var client = new UsdaFoodDataClient(httpClient, TimeProvider.System, NullLogger<UsdaFoodDataClient>.Instance);

        var food = await client.FindByBarcodeAsync(Barcode.Create("0009800800124").Value, CancellationToken.None);

        food.ShouldNotBeNull();
        food.Name.ShouldBe("NUTELLA FERRERO WITH BREADSTICKS");
        food.Brand.ShouldBe("NUTELLA FERRERO");
        food.DefaultServing.ShouldBe(new Serving("1 CONTAINER (52 g)", 52m));
    }

    [Fact]
    public async Task SearchAsync_RateLimited_ReturnsNothing()
    {
        using var httpClient = StubHttp.Returning("""{"error":{"code":"OVER_RATE_LIMIT"}}""", HttpStatusCode.TooManyRequests);
        var client = new UsdaFoodDataClient(httpClient, TimeProvider.System, NullLogger<UsdaFoodDataClient>.Instance);

        (await client.SearchAsync("banana", 20, CancellationToken.None)).ShouldBeEmpty();
    }
}

internal static class StubHttp
{
    public static HttpClient Returning(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        Create(_ => new HttpResponseMessage(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    public static HttpClient Throwing(Exception exception) => Create(_ => throw exception);

    private static HttpClient Create(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new Handler(respond)) { BaseAddress = new Uri("https://food-database.test/") };

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
