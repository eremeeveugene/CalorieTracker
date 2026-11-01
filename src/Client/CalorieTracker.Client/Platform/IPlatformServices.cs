namespace CalorieTracker.Client.Platform;

// GoF Abstract Factory: each platform head (Desktop, Android, iOS, Browser) supplies
// its own family of platform-specific services
public interface IPlatformServices
{
    ITokenStore CreateTokenStore();
}

// Used by heads without secure storage yet: the user signs in again after a restart.
// Browser stays on this deliberately; tokens in web storage are readable by injected scripts.
public sealed class DefaultPlatformServices : IPlatformServices
{
    public ITokenStore CreateTokenStore() => new InMemoryTokenStore();
}
