using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CalorieTracker.Gateway.IntegrationTests;

// Stands in for the Identity service on a real port: publishes a test signing key
// and answers every other request with the path it received
public sealed class FakeIdentityServer : IAsyncDisposable
{
    public const string Issuer = "test-identity";
    public const string Audience = "calorie-tracker";

    private readonly WebApplication _app;
    private readonly RSA _rsa;
    private readonly RsaSecurityKey _signingKey;

    private FakeIdentityServer(WebApplication app, RSA rsa, RsaSecurityKey signingKey)
    {
        _app = app;
        _rsa = rsa;
        _signingKey = signingKey;
    }

    public Uri BaseAddress => new(_app.Urls.First());

    public static async Task<FakeIdentityServer> StartAsync()
    {
        var rsa = RSA.Create(2048);
        var signingKey = new RsaSecurityKey(rsa) { KeyId = "test-key" };
        var publicKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(
            new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false)) { KeyId = signingKey.KeyId });

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();

        app.MapGet("/.well-known/openid-configuration", (HttpRequest request) => Results.Json(new Dictionary<string, string>
        {
            ["issuer"] = Issuer,
            ["jwks_uri"] = $"{request.Scheme}://{request.Host}/.well-known/jwks.json",
        }));
        app.MapGet("/.well-known/jwks.json", () => Results.Json(new Dictionary<string, object>
        {
            ["keys"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["kty"] = "RSA",
                    ["use"] = "sig",
                    ["alg"] = SecurityAlgorithms.RsaSha256,
                    ["kid"] = publicKey.Kid,
                    ["n"] = publicKey.N,
                    ["e"] = publicKey.E,
                },
            },
        }));
        app.Map("/{**path}", (HttpRequest request) => Results.Text(request.Path.Value));

        await app.StartAsync();
        return new FakeIdentityServer(app, rsa, signingKey);
    }

    public string CreateAccessToken(DateTime? expiresAt = null, SecurityKey? signingKey = null)
    {
        var expires = expiresAt ?? DateTime.UtcNow.AddMinutes(15);
        var issuedAt = expires.AddMinutes(-15);

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expires,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())]),
            SigningCredentials = new SigningCredentials(signingKey ?? _signingKey, SecurityAlgorithms.RsaSha256),
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        _rsa.Dispose();
    }
}
