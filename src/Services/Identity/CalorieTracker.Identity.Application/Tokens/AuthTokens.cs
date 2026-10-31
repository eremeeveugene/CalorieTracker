namespace CalorieTracker.Identity.Application.Tokens;

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public override string ToString() =>
        $"{nameof(AuthTokens)} {{ AccessTokenExpiresAt = {AccessTokenExpiresAt}, RefreshTokenExpiresAt = {RefreshTokenExpiresAt} }}";
}
