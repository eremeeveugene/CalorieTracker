namespace CalorieTracker.Identity.Application.Tokens;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshTokens";

    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(30);
}
