namespace CalorieTracker.Contracts.Identity;

// ToString overrides keep passwords and tokens out of logs

public sealed record RegisterRequest(string Email, string Password)
{
    public override string ToString() => $"{nameof(RegisterRequest)} {{ Email = {Email} }}";
}

public sealed record LoginRequest(string Email, string Password)
{
    public override string ToString() => $"{nameof(LoginRequest)} {{ Email = {Email} }}";
}

public sealed record RefreshRequest(string RefreshToken)
{
    public override string ToString() => nameof(RefreshRequest);
}

public sealed record AuthTokensResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public override string ToString() =>
        $"{nameof(AuthTokensResponse)} {{ AccessTokenExpiresAt = {AccessTokenExpiresAt}, RefreshTokenExpiresAt = {RefreshTokenExpiresAt} }}";
}
