using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Contracts.Identity;
using CalorieTracker.Identity.Application.Login;
using CalorieTracker.Identity.Application.Refresh;
using CalorieTracker.Identity.Application.Register;
using CalorieTracker.Identity.Application.Tokens;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CalorieTracker.Identity.Api.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/v1/auth").WithTags("Auth");

        auth.MapPost("/register", RegisterAsync);
        auth.MapPost("/login", LoginAsync);
        auth.MapPost("/refresh", RefreshAsync);

        return app;
    }

    private static async Task<Results<Ok<AuthTokensResponse>, ProblemHttpResult>> RegisterAsync(
        RegisterRequest request,
        ICommandHandler<RegisterCommand, Result<AuthTokens>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RegisterCommand(request.Email, request.Password), cancellationToken);
        return ToHttpResult(result);
    }

    private static async Task<Results<Ok<AuthTokensResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        ICommandHandler<LoginCommand, Result<AuthTokens>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LoginCommand(request.Email, request.Password), cancellationToken);
        return ToHttpResult(result);
    }

    private static async Task<Results<Ok<AuthTokensResponse>, ProblemHttpResult>> RefreshAsync(
        RefreshRequest request,
        ICommandHandler<RefreshTokensCommand, Result<AuthTokens>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RefreshTokensCommand(request.RefreshToken), cancellationToken);
        return ToHttpResult(result);
    }

    private static Results<Ok<AuthTokensResponse>, ProblemHttpResult> ToHttpResult(Result<AuthTokens> result)
    {
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        var tokens = result.Value;
        return TypedResults.Ok(new AuthTokensResponse(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt));
    }
}
