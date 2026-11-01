using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.BuildingBlocks.Web;
using CalorieTracker.Catalog.Application.Barcodes;
using CalorieTracker.Catalog.Application.Foods;
using CalorieTracker.Catalog.Application.Search;
using CalorieTracker.Catalog.Domain.Foods;
using CalorieTracker.Contracts.Catalog;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CalorieTracker.Catalog.Api.Endpoints;

// Every endpoint requires a signed-in user (fallback authorization policy)
internal static class FoodEndpoints
{
    private const int DefaultLimit = 20;

    public static IEndpointRouteBuilder MapFoodEndpoints(this IEndpointRouteBuilder app)
    {
        var foods = app.MapGroup("/v1/foods").WithTags("Foods");

        foods.MapGet("/", SearchAsync);
        foods.MapGet("/barcode/{code}", GetByBarcodeAsync);
        foods.MapGet("/{id:guid}", GetByIdAsync);

        return app;
    }

    private static async Task<Results<Ok<FoodResponse[]>, ProblemHttpResult>> SearchAsync(
        string? search,
        int? limit,
        IQueryHandler<SearchFoodsQuery, Result<IReadOnlyList<Food>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new SearchFoodsQuery(search ?? string.Empty, limit ?? DefaultLimit), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value.Select(ToResponse).ToArray())
            : result.Error.ToProblem();
    }

    private static async Task<Results<Ok<FoodResponse>, ProblemHttpResult>> GetByBarcodeAsync(
        string code,
        IQueryHandler<GetFoodByBarcodeQuery, Result<Food>> handler,
        CancellationToken cancellationToken) =>
        ToHttpResult(await handler.HandleAsync(new GetFoodByBarcodeQuery(code), cancellationToken));

    private static async Task<Results<Ok<FoodResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IQueryHandler<GetFoodQuery, Result<Food>> handler,
        CancellationToken cancellationToken) =>
        ToHttpResult(await handler.HandleAsync(new GetFoodQuery(id), cancellationToken));

    private static Results<Ok<FoodResponse>, ProblemHttpResult> ToHttpResult(Result<Food> result) =>
        result.IsSuccess ? TypedResults.Ok(ToResponse(result.Value)) : result.Error.ToProblem();

    private static FoodResponse ToResponse(Food food) => new(
        food.Id,
        food.Name,
        food.Brand,
        food.Barcode?.PrintedForm,
        food.Source.ToString(),
        new NutrientsResponse(
            food.Per100Grams.Calories,
            food.Per100Grams.Protein,
            food.Per100Grams.Carbohydrates,
            food.Per100Grams.Fat),
        food.DefaultServing is { } serving ? new ServingResponse(serving.Description, serving.Grams) : null);
}
