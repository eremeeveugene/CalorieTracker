using CalorieTracker.BuildingBlocks.Messaging;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Identity.Application.Login;
using CalorieTracker.Identity.Application.Refresh;
using CalorieTracker.Identity.Application.Register;
using CalorieTracker.Identity.Application.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CalorieTracker.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<TokenPairIssuer>();

        services.AddScoped<ICommandHandler<RegisterCommand, Result<AuthTokens>>, RegisterCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, Result<AuthTokens>>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshTokensCommand, Result<AuthTokens>>, RefreshTokensCommandHandler>();

        return services;
    }
}
