using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Identity.Application.Abstractions;
using CalorieTracker.Identity.Application.Tokens;

namespace CalorieTracker.Identity.Application.Login;

public sealed record LoginCommand(string Email, string Password)
{
    public override string ToString() => $"{nameof(LoginCommand)} {{ Email = {Email} }}";
}

internal sealed class LoginCommandHandler(IUserAccounts userAccounts, TokenPairIssuer tokenPairIssuer)
    : ICommandHandler<LoginCommand, Result<AuthTokens>>
{
    public async Task<Result<AuthTokens>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var verified = await userAccounts.VerifyCredentialsAsync(command.Email, command.Password);
        if (verified.IsFailure)
        {
            return Result.Failure<AuthTokens>(verified.Error);
        }

        return Result.Success(await tokenPairIssuer.IssueAsync(verified.Value, cancellationToken));
    }
}
