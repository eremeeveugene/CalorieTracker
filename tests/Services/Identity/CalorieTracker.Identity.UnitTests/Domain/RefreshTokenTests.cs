using CalorieTracker.Identity.Domain.RefreshTokens;

namespace CalorieTracker.Identity.UnitTests.Domain;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    [Fact]
    public void Issue_SetsExpiryFromLifetime()
    {
        var token = RefreshToken.Issue(Guid.CreateVersion7(), "hash", Now, Lifetime);

        token.ExpiresAt.ShouldBe(Now + Lifetime);
        token.IsRevoked.ShouldBeFalse();
    }

    [Fact]
    public void Issue_NonPositiveLifetime_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => RefreshToken.Issue(Guid.CreateVersion7(), "hash", Now, TimeSpan.Zero));
    }

    [Fact]
    public void IsActiveAt_AtExpiry_IsFalse()
    {
        var token = RefreshToken.Issue(Guid.CreateVersion7(), "hash", Now, Lifetime);

        token.IsActiveAt(Now + Lifetime - TimeSpan.FromSeconds(1)).ShouldBeTrue();
        token.IsActiveAt(Now + Lifetime).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_MakesTokenInactive()
    {
        var token = RefreshToken.Issue(Guid.CreateVersion7(), "hash", Now, Lifetime);

        token.Revoke(Now);

        token.IsRevoked.ShouldBeTrue();
        token.IsActiveAt(Now).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_AlreadyRevoked_KeepsFirstRevocationTime()
    {
        var token = RefreshToken.Issue(Guid.CreateVersion7(), "hash", Now, Lifetime);

        token.Revoke(Now);
        token.Revoke(Now.AddHours(1));

        token.RevokedAt.ShouldBe(Now);
    }
}
