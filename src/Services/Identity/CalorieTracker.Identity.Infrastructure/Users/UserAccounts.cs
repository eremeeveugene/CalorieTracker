using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Identity.Application;
using CalorieTracker.Identity.Application.Abstractions;
using CalorieTracker.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace CalorieTracker.Identity.Infrastructure.Users;

// GoF Adapter: exposes ASP.NET Core Identity's UserManager through the application's IUserAccounts port
internal sealed class UserAccounts(UserManager<ApplicationUser> userManager) : IUserAccounts
{
    public async Task<Result<UserAccount>> CreateAsync(string email, string password)
    {
        var user = new ApplicationUser { Id = Guid.CreateVersion7(), UserName = email, Email = email };

        var created = await userManager.CreateAsync(user, password);
        if (created.Succeeded)
        {
            return Result.Success(ToAccount(user));
        }

        if (created.Errors.Any(IsDuplicate))
        {
            return Result.Failure<UserAccount>(IdentityErrors.EmailTaken);
        }

        var description = string.Join(" ", created.Errors.Select(error => error.Description));
        return Result.Failure<UserAccount>(IdentityErrors.InvalidRegistration(description));
    }

    public async Task<Result<UserAccount>> VerifyCredentialsAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Failure<UserAccount>(IdentityErrors.InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<UserAccount>(IdentityErrors.LockedOut);
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return Result.Failure<UserAccount>(IdentityErrors.InvalidCredentials);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return Result.Success(ToAccount(user));
    }

    public async Task<UserAccount?> FindByIdAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToAccount(user);
    }

    private static bool IsDuplicate(IdentityError error) =>
        error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName);

    private static UserAccount ToAccount(ApplicationUser user) => new(user.Id, user.Email!);
}
