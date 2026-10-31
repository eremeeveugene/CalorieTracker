using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CalorieTracker.Identity.Infrastructure.Tokens;

// Holds the RSA key that signs access tokens. Only Identity has the private key;
// other services validate tokens with the public half.
public sealed class SigningKeyProvider : IDisposable
{
    private const int DevelopmentKeySize = 2048;

    private readonly RSA _rsa;

    public SigningKeyProvider(IOptions<JwtOptions> options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        _rsa = CreateKey(options.Value.SigningKey, environment);

        var key = new RsaSecurityKey(_rsa);
        key.KeyId = Base64UrlEncoder.Encode(key.ComputeJwkThumbprint());

        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    public SigningCredentials SigningCredentials { get; }

    public void Dispose() => _rsa.Dispose();

    private static RSA CreateKey(string? pem, IHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(pem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }

        if (environment.IsDevelopment())
        {
            // Throwaway key: tokens stop validating after a restart and clients refresh them
            return RSA.Create(DevelopmentKeySize);
        }

        throw new InvalidOperationException(
            $"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)} must be configured outside Development.");
    }
}
