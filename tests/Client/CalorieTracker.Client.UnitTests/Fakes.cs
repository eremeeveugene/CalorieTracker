using System.Net;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Client.Api;
using CalorieTracker.Client.Navigation;
using CalorieTracker.Client.Platform;
using CalorieTracker.Client.ViewModels;
using CalorieTracker.Contracts.Identity;

namespace CalorieTracker.Client.UnitTests;

internal static class TestClock
{
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class FakeAuthApi : IAuthApi
{
    private int _refreshCalls;

    public static readonly Error Rejected = Error.Unauthorized("Identity.InvalidCredentials", "The email or password is incorrect.");

    public Result<AuthTokensResponse> LoginResult { get; set; } = Result.Success(Tokens("login"));

    public Result<AuthTokensResponse>? RefreshFailure { get; set; }

    public int RefreshCalls => _refreshCalls;

    public static AuthTokensResponse Tokens(string name, TimeSpan? accessTokenLifetime = null) =>
        new($"access-{name}", TestClock.Now + (accessTokenLifetime ?? TimeSpan.FromMinutes(15)), $"refresh-{name}", TestClock.Now.AddDays(30));

    public Task<Result<AuthTokensResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(LoginResult);

    public Task<Result<AuthTokensResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(LoginResult);

    public async Task<Result<AuthTokensResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var call = Interlocked.Increment(ref _refreshCalls);

        // Slow enough for concurrent callers to pile up behind the session's refresh lock
        await Task.Delay(50, cancellationToken);
        return RefreshFailure ?? Result.Success(Tokens($"refresh-{call}"));
    }
}

internal sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}

internal sealed class RecordingNavigator : INavigator
{
    public Type? CurrentPageType { get; private set; }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase => CurrentPageType = typeof(TViewModel);
}

internal sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<string?> SentAuthorizations { get; } = [];

    public static StubHttpHandler Throwing(Exception exception) => new(_ => throw exception);

    public static StubHttpHandler Returning(HttpStatusCode statusCode, string? json = null) =>
        new(_ => new HttpResponseMessage(statusCode)
        {
            Content = json is null ? null : new StringContent(json, System.Text.Encoding.UTF8, "application/problem+json"),
        });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentAuthorizations.Add(request.Headers.Authorization?.Parameter);
        return Task.FromResult(respond(request));
    }
}
