using CalorieTracker.Identity.Application.Abstractions;
using CalorieTracker.Identity.Application.Tokens;
using CalorieTracker.Identity.Infrastructure.Persistence;
using CalorieTracker.Identity.Infrastructure.Tokens;
using CalorieTracker.Identity.Infrastructure.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CalorieTracker.Identity.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseName = "identitydb";

    private const int MinimumPasswordLength = 10;
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static IHostApplicationBuilder AddIdentityInfrastructure(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddNpgsqlDbContext<IdentityServiceDbContext>(DatabaseName);

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Length beats composition rules (NIST SP 800-63B)
                options.Password.RequiredLength = MinimumPasswordLength;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = MaxFailedLoginAttempts;
                options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
            })
            .AddEntityFrameworkStores<IdentityServiceDbContext>();

        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.Configure<RefreshTokenOptions>(builder.Configuration.GetSection(RefreshTokenOptions.SectionName));

        builder.Services.AddSingleton<SigningKeyProvider>();
        builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        builder.Services.AddScoped<IUserAccounts, UserAccounts>();
        builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        builder.Services.AddScoped<IUnitOfWork>(services => services.GetRequiredService<IdentityServiceDbContext>());

        return builder;
    }
}
