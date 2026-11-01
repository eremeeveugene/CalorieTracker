using CalorieTracker.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CalorieTracker.Gateway.IntegrationTests;

public sealed class GatewayTests(GatewayFactory factory) : IClassFixture<GatewayFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task AuthRoute_IsPublicAndForwardedWithoutPrefix()
    {
        using var response = await _client.PostAsJsonAsync("/identity/v1/auth/login", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("/v1/auth/login");
    }

    [Fact]
    public async Task ProtectedRoute_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await _client.GetAsync("/protected/data");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedRoute_WithValidToken_IsForwarded()
    {
        using var response = await SendWithTokenAsync(factory.Identity.CreateAccessToken());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("/protected/data");
    }

    [Fact]
    public async Task ProtectedRoute_WithExpiredToken_ReturnsUnauthorized()
    {
        // Past the default five-minute clock skew
        var token = factory.Identity.CreateAccessToken(expiresAt: DateTime.UtcNow.AddMinutes(-10));

        using var response = await SendWithTokenAsync(token);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedRoute_WithTokenSignedByUnknownKey_ReturnsUnauthorized()
    {
        using var attackerRsa = RSA.Create(2048);
        var token = factory.Identity.CreateAccessToken(signingKey: new RsaSecurityKey(attackerRsa) { KeyId = "test-key" });

        using var response = await SendWithTokenAsync(token);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthRoute_OverLimit_ReturnsTooManyRequests()
    {
        using var limitedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:Auth:PermitLimit"] = "2" })));
        using var client = limitedFactory.CreateClient();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var allowed = await client.PostAsJsonAsync("/identity/v1/auth/login", new { });
            allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var rejected = await client.PostAsJsonAsync("/identity/v1/auth/login", new { });

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter.ShouldNotBeNull();
        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe("Gateway.TooManyRequests");
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/protected/data");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }
}
