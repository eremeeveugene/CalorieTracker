using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CalorieTracker.BuildingBlocks.Results;

namespace CalorieTracker.Client.Api;

internal static class ApiErrors
{
    public static readonly Error ServerUnreachable =
        Error.Failure("Network.Unreachable", "Can't reach the server. Check your connection and try again.");

    public static readonly Error UnexpectedResponse =
        Error.Failure("Network.UnexpectedResponse", "The server sent an unexpected response. Try again.");
}

// Turns HTTP responses into Results; error bodies are ProblemDetails whose title is the error code
internal static class ApiResponses
{
    public static async Task<Result<TValue>> ReadAsync<TValue>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<TValue>(await ReadErrorAsync(response, cancellationToken));
        }

        var value = await response.Content.ReadFromJsonAsync<TValue>(cancellationToken);
        return value is null
            ? Result.Failure<TValue>(ApiErrors.UnexpectedResponse)
            : Result.Success(value);
    }

    private static async Task<Error> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await TryReadProblemAsync(response, cancellationToken);
        var statusCode = response.StatusCode;

        return new Error(
            problem?.Title ?? $"Http.{(int)statusCode}",
            problem?.Detail ?? DefaultDescription(statusCode),
            ToErrorType(statusCode));
    }

    private static async Task<ProblemResponse?> TryReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemResponse>(cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            // Not JSON at all
            return null;
        }
    }

    private static ErrorType ToErrorType(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => ErrorType.Validation,
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ErrorType.Unauthorized,
        HttpStatusCode.NotFound => ErrorType.NotFound,
        HttpStatusCode.Conflict => ErrorType.Conflict,
        _ => ErrorType.Failure,
    };

    private static string DefaultDescription(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.TooManyRequests => "Too many attempts. Wait a minute and try again.",
        HttpStatusCode.Unauthorized => "Your session has ended. Sign in again.",
        _ => "Something went wrong. Try again.",
    };

    private sealed record ProblemResponse(string? Title, string? Detail);
}
