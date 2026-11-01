using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.Hosting;

// Every service validates access tokens itself, even behind the gateway (defense in depth).
// Tokens are checked against Identity's public keys, fetched from its discovery document.
public static class AuthenticationExtensions
{
    public const string ConfigurationSection = "Authentication:Jwt";

    private const string DefaultMetadataAddress = "https+http://identity/.well-known/openid-configuration";
    private const string DefaultAudience = "calorie-tracker";
    private const string BackchannelClientName = "jwt-metadata";

    public static TBuilder AddJwtAuthentication<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var section = builder.Configuration.GetSection(ConfigurationSection);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddHttpClient(BackchannelClientName);
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IHttpClientFactory>((options, httpClientFactory) =>
            {
                options.MetadataAddress = section["MetadataAddress"] ?? DefaultMetadataAddress;
                options.Audience = section["Audience"] ?? DefaultAudience;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.RsaSha256];

                // The factory client resolves service names such as "identity" through service discovery.
                // Service-to-service calls stay on the private network; TLS ends at the edge proxy.
                options.Backchannel = httpClientFactory.CreateClient(BackchannelClientName);
                options.RequireHttpsMetadata = false;
            });

        // Deny by default: endpoints must opt out with AllowAnonymous
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return builder;
    }
}
