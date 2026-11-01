using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Identity.Application.Abstractions;
using CalorieTracker.Identity.Application.Tokens;

namespace CalorieTracker.Identity.Application.Refresh;

public sealed record RefreshTokensCommand(string RefreshToken)
{
    public override string ToString() => nameof(RefreshTokensCommand);
}

internal sealed class RefreshTokensCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IUserAccounts userAccounts,
    TokenPairIssuer tokenPairIssuer,
    TimeProvider timeProvider)
    : ICommandHandler<RefreshTokensCommand, Result<AuthTokens>>
{
    public async Task<Result<AuthTokens>> HandleAsync(RefreshTokensCommand command, CancellationToken cancellationToken)
    {
        // A field missing from the JSON body arrives as null despite the non-nullable type
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return Result.Failure<AuthTokens>(IdentityErrors.InvalidRefreshToken);
        }

        var now = timeProvider.GetUtcNow();
        var refreshToken = await refreshTokens.FindByHashAsync(RefreshTokenSecret.Hash(command.RefreshToken), cancellationToken);
        if (refreshToken is null)
        {
            return Result.Failure<AuthTokens>(IdentityErrors.InvalidRefreshToken);
        }

        if (refreshToken.IsRevoked)
        {
            // A rotated token was used again, so it has leaked: end every session of this user
            await refreshTokens.RevokeAllAsync(refreshToken.UserId, now, cancellationToken);
            return Result.Failure<AuthTokens>(IdentityErrors.InvalidRefreshToken);
        }

        var user = refreshToken.IsActiveAt(now) ? await userAccounts.FindByIdAsync(refreshToken.UserId) : null;
        if (user is null)
        {
            return Result.Failure<AuthTokens>(IdentityErrors.InvalidRefreshToken);
        }

        refreshToken.Revoke(now);
        return Result.Success(await tokenPairIssuer.IssueAsync(user, cancellationToken));
    }
}
