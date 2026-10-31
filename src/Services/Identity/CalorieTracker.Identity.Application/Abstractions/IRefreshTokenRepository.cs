using CalorieTracker.Identity.Domain.RefreshTokens;

namespace CalorieTracker.Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken refreshToken);

    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}
