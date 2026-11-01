using System.Net.Http.Json;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Contracts.Identity;

namespace CalorieTracker.Client.Api;

// Anonymous calls only, so this client has no AuthenticationHandler (refreshing must not recurse)
internal sealed class AuthApi(HttpClient httpClient) : IAuthApi
{
    public Task<Result<AuthTokensResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        PostAsync("identity/v1/auth/register", request, cancellationToken);

    public Task<Result<AuthTokensResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        PostAsync("identity/v1/auth/login", request, cancellationToken);

    public Task<Result<AuthTokensResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken) =>
        PostAsync("identity/v1/auth/refresh", request, cancellationToken);

    private async Task<Result<AuthTokensResponse>> PostAsync<TRequest>(
        string path, TRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(path, request, cancellationToken);
            return await ApiResponses.ReadAsync<AuthTokensResponse>(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<AuthTokensResponse>(ApiErrors.ServerUnreachable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout rather than the caller cancelling
            return Result.Failure<AuthTokensResponse>(ApiErrors.ServerUnreachable);
        }
    }
}
