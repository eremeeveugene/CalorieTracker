namespace CalorieTracker.Client;

public sealed record ClientSettings(Uri GatewayAddress)
{
    // Gateway's https launch profile, used when the client runs outside the AppHost
    private const string DevelopmentGatewayAddress = "https://localhost:7231";

    // The AppHost passes the gateway address when it launches the desktop client
    public static ClientSettings FromEnvironment() => new(new Uri(
        Environment.GetEnvironmentVariable("services__gateway__https__0")
        ?? Environment.GetEnvironmentVariable("services__gateway__http__0")
        ?? DevelopmentGatewayAddress));
}
