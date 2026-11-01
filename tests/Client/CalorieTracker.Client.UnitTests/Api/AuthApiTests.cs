using System.Net;
using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Client.Api;
using CalorieTracker.Contracts.Identity;

namespace CalorieTracker.Client.UnitTests.Api;

public class AuthApiTests
{
    [Fact]
    public async Task LoginAsync_ProblemResponse_BecomesErrorWithServerCodeAndMessage()
    {
        const string problem = """{"title":"Identity.EmailTaken","detail":"An account with this email already exists.","status":409}""";

        var result = await LoginAsync(StubHttpHandler.Returning(HttpStatusCode.Conflict, problem));

        result.Error.Code.ShouldBe("Identity.EmailTaken");
        result.Error.Description.ShouldBe("An account with this email already exists.");
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public async Task LoginAsync_TooManyRequestsWithoutBody_GivesReadableMessage()
    {
        var result = await LoginAsync(StubHttpHandler.Returning(HttpStatusCode.TooManyRequests));

        result.Error.Code.ShouldBe("Http.429");
        result.Error.Description.ShouldBe("Too many attempts. Wait a minute and try again.");
    }

    [Fact]
    public async Task LoginAsync_ServerDown_ReturnsUnreachable()
    {
        var result = await LoginAsync(StubHttpHandler.Throwing(new HttpRequestException("Connection refused")));

        result.Error.ShouldBe(ApiErrors.ServerUnreachable);
    }

    private static async Task<Result<AuthTokensResponse>> LoginAsync(StubHttpHandler server)
    {
        using var httpClient = new HttpClient(server) { BaseAddress = new Uri("https://gateway.test") };
        return await new AuthApi(httpClient).LoginAsync(new LoginRequest("user@example.com", "password"), CancellationToken.None);
    }
}
