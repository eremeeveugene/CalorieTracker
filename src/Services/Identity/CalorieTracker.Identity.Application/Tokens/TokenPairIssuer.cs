using CalorieTracker.Identity.Application.Abstractions;
using CalorieTracker.Identity.Domain.RefreshTokens;
using Microsoft.Extensions.Options;

namespace CalorieTracker.Identity.Application.Tokens;

internal sealed class TokenPairIssuer(
    IAccessTokenIssuer accessTokenIssuer,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<RefreshTokenOptions> options)
{
    public async Task<AuthTokens> IssueAsync(UserAccount user, CancellationToken cancellationToken)
    {
        var accessToken = accessTokenIssuer.Issue(user);
        var refreshTokenValue = RefreshTokenSecret.Generate();
        var refreshToken = RefreshToken.Issue(
            user.Id,
            RefreshTokenSecret.Hash(refreshTokenValue),
            timeProvider.GetUtcNow(),
            options.Value.Lifetime);

        refreshTokens.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokens(accessToken.Value, accessToken.ExpiresAt, refreshTokenValue, refreshToken.ExpiresAt);
    }
}
