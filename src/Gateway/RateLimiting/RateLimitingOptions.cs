namespace CalorieTracker.Gateway.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    // Login, register and refresh per client address: slows password guessing before account lockout
    public FixedWindowSettings Auth { get; set; } = new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) };

    // Every request per signed-in user, or per client address for anonymous requests
    public FixedWindowSettings Global { get; set; } = new() { PermitLimit = 300, Window = TimeSpan.FromMinutes(1) };
}

public sealed class FixedWindowSettings
{
    public int PermitLimit { get; set; }

    public TimeSpan Window { get; set; }
}
