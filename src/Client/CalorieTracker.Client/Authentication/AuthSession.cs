using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Client.Api;
using CalorieTracker.Client.Platform;
using CalorieTracker.Contracts.Identity;

namespace CalorieTracker.Client.Authentication;

// The signed-in user and their tokens. Raises SignedInChanged (GoF Observer) when the user
// signs in or out, so the UI can switch screens without the session knowing about views.
public sealed class AuthSession(IAuthApi authApi, ITokenStore tokenStore, TimeProvider timeProvider) : IDisposable
{
    // Refresh slightly early so a token never expires on its way to the server
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private SignedInState? _state;

    public event EventHandler? SignedInChanged;

    public bool IsSignedIn => _state is not null;

    public string? Email => _state?.Email;

    public async Task<Result> SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        var tokens = await authApi.LoginAsync(new LoginRequest(email, password), cancellationToken);
        return await StartAsync(email, tokens, cancellationToken);
    }

    public async Task<Result> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        var tokens = await authApi.RegisterAsync(new RegisterRequest(email, password), cancellationToken);
        return await StartAsync(email, tokens, cancellationToken);
    }

    // Signs back in with the refresh token saved on this device, if there is one
    public async Task RestoreAsync(CancellationToken cancellationToken)
    {
        var stored = await tokenStore.LoadAsync(cancellationToken);
        if (stored is null)
        {
            return;
        }

        var refreshed = await authApi.RefreshAsync(new RefreshRequest(stored.RefreshToken), cancellationToken);
        if (refreshed.IsSuccess)
        {
            await SetTokensAsync(stored.Email, refreshed.Value, cancellationToken);
        }
        else if (refreshed.Error.Type == ErrorType.Unauthorized)
        {
            await tokenStore.ClearAsync(cancellationToken);
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await tokenStore.ClearAsync(cancellationToken);
        if (_state is null)
        {
            return;
        }

        _state = null;
        SignedInChanged?.Invoke(this, EventArgs.Empty);
    }

    // A usable access token, refreshed first when it is about to expire; null when signed out
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var state = _state;
        if (state is null)
        {
            return null;
        }

        var expiresSoon = state.Tokens.AccessTokenExpiresAt - RefreshMargin <= timeProvider.GetUtcNow();
        return expiresSoon
            ? await RefreshAsync(state.Tokens.AccessToken, cancellationToken)
            : state.Tokens.AccessToken;
    }

    // Replaces a stale access token. Concurrent callers share one refresh, because the server
    // treats a second use of the same refresh token as theft and ends every session.
    public async Task<string?> RefreshAsync(string staleAccessToken, CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var state = _state;
            if (state is null || state.Tokens.AccessToken != staleAccessToken)
            {
                return state?.Tokens.AccessToken;
            }

            var refreshed = await authApi.RefreshAsync(new RefreshRequest(state.Tokens.RefreshToken), cancellationToken);
            if (refreshed.IsSuccess)
            {
                await SetTokensAsync(state.Email, refreshed.Value, cancellationToken);
                return refreshed.Value.AccessToken;
            }

            // A network failure keeps the session; a rejected token ends it
            if (refreshed.Error.Type == ErrorType.Unauthorized)
            {
                await SignOutAsync(cancellationToken);
            }

            return null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void Dispose() => _refreshLock.Dispose();

    private async Task<Result> StartAsync(string email, Result<AuthTokensResponse> tokens, CancellationToken cancellationToken)
    {
        if (tokens.IsFailure)
        {
            return Result.Failure(tokens.Error);
        }

        await SetTokensAsync(email, tokens.Value, cancellationToken);
        return Result.Success();
    }

    private async Task SetTokensAsync(string email, AuthTokensResponse tokens, CancellationToken cancellationToken)
    {
        var wasSignedIn = IsSignedIn;
        _state = new SignedInState(email, tokens);
        await tokenStore.SaveAsync(new StoredSession(email, tokens.RefreshToken), cancellationToken);

        if (!wasSignedIn)
        {
            SignedInChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed record SignedInState(string Email, AuthTokensResponse Tokens);
}
