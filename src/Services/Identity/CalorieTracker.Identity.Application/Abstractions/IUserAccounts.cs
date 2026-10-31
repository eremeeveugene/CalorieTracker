using CalorieTracker.BuildingBlocks.Results;

namespace CalorieTracker.Identity.Application.Abstractions;

// Port to the user store; the infrastructure adapts ASP.NET Core Identity to it (GoF Adapter)
public interface IUserAccounts
{
    Task<Result<UserAccount>> CreateAsync(string email, string password);

    Task<Result<UserAccount>> VerifyCredentialsAsync(string email, string password);

    Task<UserAccount?> FindByIdAsync(Guid userId);
}

public sealed record UserAccount(Guid Id, string Email);
