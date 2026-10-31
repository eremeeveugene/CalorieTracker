using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Identity.Application.Abstractions;
using CalorieTracker.Identity.Domain.RefreshTokens;

namespace CalorieTracker.Identity.UnitTests.Application;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public void Add(RefreshToken refreshToken) => Tokens.Add(refreshToken);

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Tokens.SingleOrDefault(token => token.TokenHash == tokenHash));

    public Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var token in Tokens.Where(token => token.UserId == userId))
        {
            token.Revoke(now);
        }

        return Task.CompletedTask;
    }
}

internal sealed class SingleUserAccounts(UserAccount user) : IUserAccounts
{
    public Task<Result<UserAccount>> CreateAsync(string email, string password) => throw new NotSupportedException();

    public Task<Result<UserAccount>> VerifyCredentialsAsync(string email, string password) => throw new NotSupportedException();

    public Task<UserAccount?> FindByIdAsync(Guid userId) => Task.FromResult(userId == user.Id ? user : null);
}

internal sealed class FakeAccessTokenIssuer(TimeProvider timeProvider) : IAccessTokenIssuer
{
    public AccessToken Issue(UserAccount user) =>
        new($"access-token-for-{user.Id}", timeProvider.GetUtcNow().AddMinutes(15));
}

internal sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
