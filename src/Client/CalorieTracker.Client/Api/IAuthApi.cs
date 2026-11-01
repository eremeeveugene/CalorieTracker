using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Contracts.Identity;

namespace CalorieTracker.Client.Api;

// GoF Facade over the gateway: view models get Results with readable errors and never see
// HttpClient, JSON, status codes or network exceptions
public interface IAuthApi
{
    Task<Result<AuthTokensResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<Result<AuthTokensResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<Result<AuthTokensResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
}
