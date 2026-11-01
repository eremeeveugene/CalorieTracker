using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CalorieTracker.Contracts.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace CalorieTracker.Catalog.IntegrationTests;

public sealed class FoodEndpointsTests : IClassFixture<CatalogApiFactory>, IDisposable
{
    private readonly CatalogApiFactory _factory;
    private readonly HttpClient _client;

    public FoodEndpointsTests(CatalogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", factory.Identity.CreateAccessToken());
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Foods_WithoutToken_AreUnauthorized()
    {
        using var anonymous = _factory.CreateClient();

        using var response = await anonymous.GetAsync("/v1/foods?search=banana");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Barcode_FoundExternally_IsServedFromTheCatalogAfterwards()
    {
        var callsBefore = _factory.Sources.BarcodeCalls;

        var first = await _client.GetFromJsonAsync<FoodResponse>($"/v1/foods/barcode/{FakeFoodSource.Nutella}");
        var second = await _client.GetFromJsonAsync<FoodResponse>($"/v1/foods/barcode/{FakeFoodSource.Nutella}");

        first.ShouldNotBeNull();
        first.Name.ShouldBe("Nutella");
        first.Brand.ShouldBe("Ferrero");
        first.Per100Grams.ShouldBe(new NutrientsResponse(539m, 6.3m, 57.5m, 30.9m));
        second.ShouldNotBeNull();
        second.Id.ShouldBe(first.Id);
        _factory.Sources.BarcodeCalls.ShouldBe(callsBefore + 1);
    }

    [Fact]
    public async Task Barcode_Invalid_IsBadRequest()
    {
        using var response = await _client.GetAsync("/v1/foods/barcode/12345");

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Catalog.InvalidBarcode");
    }

    [Fact]
    public async Task Barcode_UnknownEverywhere_IsNotFound()
    {
        using var response = await _client.GetAsync("/v1/foods/barcode/96385074");

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "Catalog.FoodNotFound");
    }

    [Fact]
    public async Task Search_KeepsExternalResults_AndAnswersLaterSearchesFromTheCatalog()
    {
        var term = $"fruit{Guid.NewGuid():N}";
        var callsBefore = _factory.Sources.SearchCalls;

        var first = await _client.GetFromJsonAsync<FoodResponse[]>($"/v1/foods?search={term}");
        var second = await _client.GetFromJsonAsync<FoodResponse[]>($"/v1/foods?search={term}&limit=2");

        first.ShouldNotBeNull();
        first.Select(food => food.Name).ShouldBe([$"{term}, raw", $"{term}, dried"]);
        second.ShouldNotBeNull();
        second.Select(food => food.Id).ShouldBe(first.Select(food => food.Id));
        _factory.Sources.SearchCalls.ShouldBe(callsBefore + 1);
    }

    [Fact]
    public async Task Search_TreatsWildcardsLiterally()
    {
        using var response = await _client.GetAsync("/v1/foods?search=%25%25&limit=50");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var foods = await response.Content.ReadFromJsonAsync<FoodResponse[]>();
        foods.ShouldNotBeNull();
        foods.ShouldAllBe(food => food.Name.Contains("%%"));
    }

    [Fact]
    public async Task Search_TooShortTerm_IsBadRequest()
    {
        using var response = await _client.GetAsync("/v1/foods?search=a");

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Catalog.SearchTermTooShort");
    }

    [Fact]
    public async Task GetById_ReturnsStoredFood_AndUnknownIdIsNotFound()
    {
        var found = await _client.GetFromJsonAsync<FoodResponse[]>($"/v1/foods?search=veg{Guid.NewGuid():N}");
        found.ShouldNotBeNull();

        var food = await _client.GetFromJsonAsync<FoodResponse>($"/v1/foods/{found[0].Id}");
        using var unknown = await _client.GetAsync($"/v1/foods/{Guid.NewGuid()}");

        food.ShouldNotBeNull();
        food.DefaultServing.ShouldBe(new ServingResponse("1 portion", 100m));
        await ShouldBeProblemAsync(unknown, HttpStatusCode.NotFound, "Catalog.FoodNotFound");
    }

    private static async Task ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode statusCode, string code)
    {
        response.StatusCode.ShouldBe(statusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe(code);
    }
}
