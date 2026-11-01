using CalorieTracker.Client.Api;
using CalorieTracker.Client.Authentication;
using CalorieTracker.Client.Navigation;
using CalorieTracker.Client.Platform;
using CalorieTracker.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker.Client;

public static class ClientServiceCollectionExtensions
{
    // Named client for protected APIs (Phase 2); attaches and refreshes access tokens
    public const string AuthenticatedApiClient = "gateway-authenticated";

    public static IServiceCollection AddClientServices(
        this IServiceCollection services, IPlatformServices platform, ClientSettings settings)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(settings);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(platform.CreateTokenStore());
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();

        services.AddHttpClient<IAuthApi, AuthApi>(client => client.BaseAddress = settings.GatewayAddress);
        services.AddTransient<AuthenticationHandler>();
        services.AddHttpClient(AuthenticatedApiClient, client => client.BaseAddress = settings.GatewayAddress)
            .AddHttpMessageHandler<AuthenticationHandler>();

        services.AddSingleton<AuthSession>();
        services.AddSingleton<Navigator>();
        services.AddSingleton<INavigator>(provider => provider.GetRequiredService<Navigator>());

        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<HomeViewModel>();

        return services;
    }
}
