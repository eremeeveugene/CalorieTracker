using System.Net.Http.Json;
using System.Text.Json;
using CalorieTracker.Contracts.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CalorieTracker.Identity.IntegrationTests;

public sealed class WellKnownEndpointsTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task PublishedKeys_ValidateIssuedAccessTokens()
    {
        using var register = await _client.PostAsJsonAsync(
            "/v1/auth/register", new RegisterRequest($"{Guid.NewGuid():N}@example.com", "correct-horse-battery"));
        var tokens = await register.Content.ReadFromJsonAsync<AuthTokensResponse>();
        tokens.ShouldNotBeNull();

        var discovery = await _client.GetFromJsonAsync<JsonElement>("/.well-known/openid-configuration");
        var jwksUri = new Uri(discovery.GetProperty("jwks_uri").GetString()!);
        var keySet = new JsonWebKeySet(await _client.GetStringAsync(jwksUri.PathAndQuery));

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(tokens.AccessToken, new TokenValidationParameters
        {
            ValidIssuer = discovery.GetProperty("issuer").GetString(),
            ValidAudience = "calorie-tracker",
            IssuerSigningKeys = keySet.GetSigningKeys(),
        });

        validation.IsValid.ShouldBeTrue(validation.Exception?.Message);
    }

    [Fact]
    public async Task KeySet_ContainsNoPrivateKeyParts()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/.well-known/jwks.json"));
        var key = document.RootElement.GetProperty("keys")[0];

        foreach (var privatePart in new[] { "d", "p", "q", "dp", "dq", "qi" })
        {
            key.TryGetProperty(privatePart, out _).ShouldBeFalse($"JWKS exposes private parameter '{privatePart}'");
        }
    }
}
