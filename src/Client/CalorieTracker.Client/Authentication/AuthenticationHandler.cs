using System.Net;
using System.Net.Http.Headers;

namespace CalorieTracker.Client.Authentication;

// GoF Decorator around the HTTP pipeline: attaches the access token to every request and,
// when the server still answers 401, refreshes the token and retries once
internal sealed class AuthenticationHandler(AuthSession session) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await session.GetAccessTokenAsync(cancellationToken);
        if (accessToken is null)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        var refreshedToken = await session.RefreshAsync(accessToken, cancellationToken);
        if (refreshedToken is null)
        {
            return response;
        }

        response.Dispose();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
