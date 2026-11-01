using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Identity.Application.Abstractions;
using CalorieTracker.Identity.Application.Tokens;

namespace CalorieTracker.Identity.Application.Login;

public sealed record LoginCommand(string Email, string Password)
{
    // Fields missing from the JSON body arrive as null despite the non-nullable types
    public bool IsComplete => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(Password);

    public override string ToString() => $"{nameof(LoginCommand)} {{ Email = {Email} }}";
}

internal sealed class LoginCommandHandler(IUserAccounts userAccounts, TokenPairIssuer tokenPairIssuer)
    : ICommandHandler<LoginCommand, Result<AuthTokens>>
{
    public async Task<Result<AuthTokens>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsComplete)
        {
            return Result.Failure<AuthTokens>(IdentityErrors.MissingCredentials);
        }

        var verified = await userAccounts.VerifyCredentialsAsync(command.Email, command.Password);
        if (verified.IsFailure)
        {
            return Result.Failure<AuthTokens>(verified.Error);
        }

        return Result.Success(await tokenPairIssuer.IssueAsync(verified.Value, cancellationToken));
    }
}
