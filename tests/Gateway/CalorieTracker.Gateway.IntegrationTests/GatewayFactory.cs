using CalorieTracker.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CalorieTracker.Gateway.IntegrationTests;

// Runs the real gateway in front of FakeIdentityServer, plus a protected test route
public sealed class GatewayFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public FakeIdentityServer Identity { get; private set; } = null!;

    public async Task InitializeAsync() => Identity = await FakeIdentityServer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await Identity.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReverseProxy:Clusters:identity:Destinations:identity:Address"] = Identity.BaseAddress.ToString(),
            [$"{AuthenticationExtensions.ConfigurationSection}:MetadataAddress"] =
                new Uri(Identity.BaseAddress, ".well-known/openid-configuration").ToString(),
            ["ReverseProxy:Routes:protected:ClusterId"] = "identity",
            ["ReverseProxy:Routes:protected:Match:Path"] = "/protected/{**catch-all}",
            ["RateLimiting:Auth:PermitLimit"] = "1000",
        }));
}
