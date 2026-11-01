using System.Text.Json.Serialization;
using CalorieTracker.Identity.Infrastructure.Tokens;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace CalorieTracker.Identity.Api.Endpoints;

// Minimal OpenID Connect discovery: other services fetch the public signing keys from here
// and pick up a rotated key without being redeployed
internal static class WellKnownEndpoints
{
    private const string JwksPath = "/.well-known/jwks.json";

    public static IEndpointRouteBuilder MapWellKnownEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/openid-configuration", GetDiscoveryDocument).ExcludeFromDescription();
        app.MapGet(JwksPath, GetKeySet).ExcludeFromDescription();

        return app;
    }

    private static Ok<DiscoveryDocument> GetDiscoveryDocument(HttpRequest request, IOptions<JwtOptions> options) =>
        TypedResults.Ok(new DiscoveryDocument(options.Value.Issuer, $"{request.Scheme}://{request.Host}{JwksPath}"));

    private static Ok<PublicJsonWebKeySet> GetKeySet(SigningKeyProvider signingKeys) =>
        TypedResults.Ok(signingKeys.PublicKeySet);

    private sealed record DiscoveryDocument(
        [property: JsonPropertyName("issuer")] string Issuer,
        [property: JsonPropertyName("jwks_uri")] string JwksUri);
}
