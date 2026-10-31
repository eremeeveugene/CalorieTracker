using CalorieTracker.BuildingBlocks.Results;

namespace CalorieTracker.Identity.Application;

public static class IdentityErrors
{
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Identity.InvalidCredentials", "The email or password is incorrect.");

    public static readonly Error LockedOut =
        Error.Unauthorized("Identity.LockedOut", "Too many failed attempts. Try again later.");

    public static readonly Error EmailTaken =
        Error.Conflict("Identity.EmailTaken", "An account with this email already exists.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Identity.InvalidRefreshToken", "The refresh token is invalid or expired.");

    public static Error InvalidRegistration(string description) =>
        Error.Validation("Identity.InvalidRegistration", description);
}
