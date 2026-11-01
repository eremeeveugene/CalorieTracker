using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Client.Api;
using CalorieTracker.Client.Authentication;
using CalorieTracker.Client.Platform;
using CalorieTracker.Contracts.Identity;

namespace CalorieTracker.Client.UnitTests.Authentication;

public sealed class AuthSessionTests : IDisposable
{
    private readonly FakeAuthApi _api = new();
    private readonly InMemoryTokenStore _store = new();
    private readonly AuthSession _session;

    public AuthSessionTests()
    {
        _session = new AuthSession(_api, _store, new FixedTimeProvider(TestClock.Now));
    }

    public void Dispose() => _session.Dispose();

    [Fact]
    public async Task SignInAsync_Success_SavesRefreshTokenAndNotifies()
    {
        var notifications = 0;
        _session.SignedInChanged += (_, _) => notifications++;

        var result = await _session.SignInAsync("user@example.com", "password", CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _session.IsSignedIn.ShouldBeTrue();
        _session.Email.ShouldBe("user@example.com");
        notifications.ShouldBe(1);
        (await _store.LoadAsync(CancellationToken.None))!.RefreshToken.ShouldBe("refresh-login");
    }

    [Fact]
    public async Task SignInAsync_Rejected_StaysSignedOut()
    {
        _api.LoginResult = Result.Failure<AuthTokensResponse>(FakeAuthApi.Rejected);

        var result = await _session.SignInAsync("user@example.com", "wrong", CancellationToken.None);

        result.Error.ShouldBe(FakeAuthApi.Rejected);
        _session.IsSignedIn.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAccessTokenAsync_FreshToken_DoesNotRefresh()
    {
        await _session.SignInAsync("user@example.com", "password", CancellationToken.None);

        var token = await _session.GetAccessTokenAsync(CancellationToken.None);

        token.ShouldBe("access-login");
        _api.RefreshCalls.ShouldBe(0);
    }

    [Fact]
    public async Task GetAccessTokenAsync_TokenAboutToExpire_RefreshesFirst()
    {
        _api.LoginResult = Result.Success(FakeAuthApi.Tokens("login", accessTokenLifetime: TimeSpan.FromSeconds(10)));
        await _session.SignInAsync("user@example.com", "password", CancellationToken.None);

        var token = await _session.GetAccessTokenAsync(CancellationToken.None);

        token.ShouldBe("access-refresh-1");
        _api.RefreshCalls.ShouldBe(1);
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentCallers_ShareOneRefresh()
    {
        await _session.SignInAsync("user@example.com", "password", CancellationToken.None);

        var tokens = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => _session.RefreshAsync("access-login", CancellationToken.None)));

        _api.RefreshCalls.ShouldBe(1);
        tokens.ShouldAllBe(token => token == "access-refresh-1");
    }

    [Fact]
    public async Task RefreshAsync_Rejected_SignsOutAndForgetsSavedToken()
    {
        await _session.SignInAsync("user@example.com", "password", CancellationToken.None);
        _api.RefreshFailure = Result.Failure<AuthTokensResponse>(
            Error.Unauthorized("Identity.InvalidRefreshToken", "The refresh token is invalid or expired."));

        var token = await _session.RefreshAsync("access-login", CancellationToken.None);

        token.ShouldBeNull();
        _session.IsSignedIn.ShouldBeFalse();
        (await _store.LoadAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task RefreshAsync_ServerUnreachable_KeepsSession()
    {
        await _session.SignInAsync("user@example.com", "password", CancellationToken.None);
        _api.RefreshFailure = Result.Failure<AuthTokensResponse>(ApiErrors.ServerUnreachable);

        var token = await _session.RefreshAsync("access-login", CancellationToken.None);

        token.ShouldBeNull();
        _session.IsSignedIn.ShouldBeTrue();
    }

    [Fact]
    public async Task RestoreAsync_SavedToken_SignsBackIn()
    {
        await _store.SaveAsync(new StoredSession("user@example.com", "refresh-saved"), CancellationToken.None);

        await _session.RestoreAsync(CancellationToken.None);

        _session.IsSignedIn.ShouldBeTrue();
        _session.Email.ShouldBe("user@example.com");
    }

    [Fact]
    public async Task RestoreAsync_RejectedToken_ClearsSavedToken()
    {
        await _store.SaveAsync(new StoredSession("user@example.com", "refresh-revoked"), CancellationToken.None);
        _api.RefreshFailure = Result.Failure<AuthTokensResponse>(
            Error.Unauthorized("Identity.InvalidRefreshToken", "The refresh token is invalid or expired."));

        await _session.RestoreAsync(CancellationToken.None);

        _session.IsSignedIn.ShouldBeFalse();
        (await _store.LoadAsync(CancellationToken.None)).ShouldBeNull();
    }
}
