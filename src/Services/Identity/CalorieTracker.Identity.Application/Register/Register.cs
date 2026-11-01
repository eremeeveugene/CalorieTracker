using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Identity.Application.Abstractions;
using CalorieTracker.Identity.Application.Tokens;

namespace CalorieTracker.Identity.Application.Register;

public sealed record RegisterCommand(string Email, string Password)
{
    // Fields missing from the JSON body arrive as null despite the non-nullable types
    public bool IsComplete => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(Password);

    public override string ToString() => $"{nameof(RegisterCommand)} {{ Email = {Email} }}";
}

internal sealed class RegisterCommandHandler(IUserAccounts userAccounts, TokenPairIssuer tokenPairIssuer)
    : ICommandHandler<RegisterCommand, Result<AuthTokens>>
{
    public async Task<Result<AuthTokens>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsComplete)
        {
            return Result.Failure<AuthTokens>(IdentityErrors.MissingCredentials);
        }

        var created = await userAccounts.CreateAsync(command.Email, command.Password);
        if (created.IsFailure)
        {
            return Result.Failure<AuthTokens>(created.Error);
        }

        return Result.Success(await tokenPairIssuer.IssueAsync(created.Value, cancellationToken));
    }
}
