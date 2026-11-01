using System.Net;
using CalorieTracker.Client.Authentication;
using CalorieTracker.Client.Platform;

namespace CalorieTracker.Client.UnitTests.Authentication;

public sealed class AuthenticationHandlerTests : IDisposable
{
    private readonly FakeAuthApi _api = new();
    private readonly AuthSession _session;

    public AuthenticationHandlerTests()
    {
        _session = new AuthSession(_api, new InMemoryTokenStore(), new FixedTimeProvider(TestClock.Now));
    }

    public void Dispose() => _session.Dispose();

    [Fact]
    public async Task SendAsync_AttachesAccessToken()
    {
        await _session.SignInAsync("user@example.com", "password", CancellationToken.None);
        var server = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        using var response = await SendAsync(server);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        server.SentAuthorizations.ShouldBe(["access-login"]);
    }

    [Fact]
    public async Task SendAsync_Unauthorized_RefreshesAndRetriesOnce()
    {
        await _session.SignInAsync("user@example.com", "password", CancellationToken.None);

        // The server has stopped accepting the original token
        var server = new StubHttpHandler(request => new HttpResponseMessage(
            request.Headers.Authorization?.Parameter == "access-login" ? HttpStatusCode.Unauthorized : HttpStatusCode.OK));

        using var response = await SendAsync(server);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        server.SentAuthorizations.ShouldBe(["access-login", "access-refresh-1"]);
        _api.RefreshCalls.ShouldBe(1);
    }

    private async Task<HttpResponseMessage> SendAsync(StubHttpHandler server)
    {
        using var invoker = new HttpMessageInvoker(new AuthenticationHandler(_session) { InnerHandler = server });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://gateway.test/diary/v1/entries");
        return await invoker.SendAsync(request, CancellationToken.None);
    }
}
