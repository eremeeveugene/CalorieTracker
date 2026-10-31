using System.Net;
using System.Net.Http.Json;
using CalorieTracker.Contracts.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CalorieTracker.Identity.IntegrationTests;

public sealed class AuthEndpointsTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>, IDisposable
{
    private const string Password = "correct-horse-battery";

    private readonly HttpClient _client = factory.CreateClient();

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Register_ThenLogin_ReturnsTokens()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        using var response = await _client.PostAsJsonAsync("/v1/auth/login", new LoginRequest(email, Password));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        tokens.ShouldNotBeNull();
        tokens.AccessToken.ShouldNotBeNullOrWhiteSpace();
        tokens.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_ReturnsRsaSignedAccessTokenForTheUser()
    {
        var email = NewEmail();

        var tokens = await RegisterAsync(email);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(tokens.AccessToken);
        jwt.Alg.ShouldBe("RS256");
        jwt.Kid.ShouldNotBeNullOrWhiteSpace();
        jwt.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe(email);
        Guid.TryParse(jwt.Subject, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Register_ExistingEmail_ReturnsConflict()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        using var response = await _client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, Password));

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "Identity.EmailTaken");
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        using var response = await _client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(NewEmail(), "short"));

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Identity.InvalidRegistration");
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        using var response = await _client.PostAsJsonAsync("/v1/auth/login", new LoginRequest(email, "wrong-password"));

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "Identity.InvalidCredentials");
    }

    [Fact]
    public async Task Login_AfterFiveFailures_IsLockedOutEvenWithCorrectPassword()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await _client.PostAsJsonAsync("/v1/auth/login", new LoginRequest(email, "wrong-password"));
        }

        using var response = await _client.PostAsJsonAsync("/v1/auth/login", new LoginRequest(email, Password));

        await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "Identity.LockedOut");
    }

    [Fact]
    public async Task Refresh_RotatesTokens()
    {
        var tokens = await RegisterAsync(NewEmail());

        using var response = await _client.PostAsJsonAsync("/v1/auth/refresh", new RefreshRequest(tokens.RefreshToken));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rotated = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        rotated.ShouldNotBeNull();
        rotated.RefreshToken.ShouldNotBe(tokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ReusedToken_RevokesTheReplacementToo()
    {
        var original = await RegisterAsync(NewEmail());
        using var first = await _client.PostAsJsonAsync("/v1/auth/refresh", new RefreshRequest(original.RefreshToken));
        var replacement = await first.Content.ReadFromJsonAsync<AuthTokensResponse>();
        replacement.ShouldNotBeNull();

        using var reuse = await _client.PostAsJsonAsync("/v1/auth/refresh", new RefreshRequest(original.RefreshToken));
        using var afterReuse = await _client.PostAsJsonAsync("/v1/auth/refresh", new RefreshRequest(replacement.RefreshToken));

        await ShouldBeProblemAsync(reuse, HttpStatusCode.Unauthorized, "Identity.InvalidRefreshToken");
        await ShouldBeProblemAsync(afterReuse, HttpStatusCode.Unauthorized, "Identity.InvalidRefreshToken");
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    private static async Task ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode statusCode, string code)
    {
        response.StatusCode.ShouldBe(statusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe(code);
    }

    private async Task<AuthTokensResponse> RegisterAsync(string email)
    {
        using var response = await _client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, Password));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        tokens.ShouldNotBeNull();
        return tokens;
    }
}
