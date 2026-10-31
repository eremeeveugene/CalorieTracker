using CalorieTracker.BuildingBlocks.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CalorieTracker.Identity.Api.Endpoints;

internal static class ErrorExtensions
{
    public static ProblemHttpResult ToProblem(this Error error) =>
        TypedResults.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: ToStatusCode(error.Type));

    private static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };
}
