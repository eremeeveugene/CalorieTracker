using CalorieTracker.Client.Platform;
using CalorieTracker.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker.Client.UnitTests;

public class ClientServiceCollectionTests
{
    [Fact]
    public async Task AddClientServices_ResolvesShellAndEveryPage()
    {
        await using var services = new ServiceCollection()
            .AddClientServices(new DefaultPlatformServices(), new ClientSettings(new Uri("https://gateway.test")))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        services.GetRequiredService<MainViewModel>().ShouldNotBeNull();
        services.GetRequiredService<LoginViewModel>().ShouldNotBeNull();
        services.GetRequiredService<RegisterViewModel>().ShouldNotBeNull();
        services.GetRequiredService<HomeViewModel>().ShouldNotBeNull();
    }
}
