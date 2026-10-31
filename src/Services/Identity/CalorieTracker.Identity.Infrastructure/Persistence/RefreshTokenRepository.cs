using CalorieTracker.Identity.Application.Abstractions;
using CalorieTracker.Identity.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace CalorieTracker.Identity.Infrastructure.Persistence;

internal sealed class RefreshTokenRepository(IdentityServiceDbContext dbContext) : IRefreshTokenRepository
{
    public void Add(RefreshToken refreshToken) => dbContext.RefreshTokens.Add(refreshToken);

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
}
