using CalorieTracker.Identity.Application;
using CalorieTracker.Identity.Application.Abstractions;
using CalorieTracker.Identity.Application.Refresh;
using CalorieTracker.Identity.Application.Tokens;
using CalorieTracker.Identity.Domain.RefreshTokens;
using Microsoft.Extensions.Options;

namespace CalorieTracker.Identity.UnitTests.Application;

public class RefreshTokensCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly UserAccount _user = new(Guid.CreateVersion7(), "user@example.com");
    private readonly InMemoryRefreshTokenRepository _repository = new();
    private readonly FixedTimeProvider _time = new(Now);
    private readonly RefreshTokensCommandHandler _handler;

    public RefreshTokensCommandHandlerTests()
    {
        var options = Options.Create(new RefreshTokenOptions());
        var tokenPairIssuer = new TokenPairIssuer(
            new FakeAccessTokenIssuer(_time), _repository, new NoOpUnitOfWork(), _time, options);

        _handler = new RefreshTokensCommandHandler(_repository, new SingleUserAccounts(_user), tokenPairIssuer, _time);
    }

    [Fact]
    public async Task HandleAsync_ActiveToken_RevokesItAndIssuesReplacement()
    {
        var current = StoreToken("current-token");

        var result = await _handler.HandleAsync(new RefreshTokensCommand("current-token"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.RefreshToken.ShouldNotBe("current-token");
        current.IsRevoked.ShouldBeTrue();
        _repository.Tokens.Count(token => !token.IsRevoked).ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ReusedToken_RevokesEverySessionOfTheUser()
    {
        StoreToken("leaked-token");
        await _handler.HandleAsync(new RefreshTokensCommand("leaked-token"), CancellationToken.None);

        var reuse = await _handler.HandleAsync(new RefreshTokensCommand("leaked-token"), CancellationToken.None);

        reuse.Error.ShouldBe(IdentityErrors.InvalidRefreshToken);
        _repository.Tokens.ShouldAllBe(token => token.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_Fails()
    {
        StoreToken("expired-token");
        _time.Now = Now + TimeSpan.FromDays(31);

        var result = await _handler.HandleAsync(new RefreshTokensCommand("expired-token"), CancellationToken.None);

        result.Error.ShouldBe(IdentityErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task HandleAsync_UnknownToken_Fails()
    {
        var result = await _handler.HandleAsync(new RefreshTokensCommand("unknown-token"), CancellationToken.None);

        result.Error.ShouldBe(IdentityErrors.InvalidRefreshToken);
    }

    private RefreshToken StoreToken(string value)
    {
        var token = RefreshToken.Issue(_user.Id, RefreshTokenSecret.Hash(value), Now, TimeSpan.FromDays(30));
        _repository.Add(token);
        return token;
    }
}
