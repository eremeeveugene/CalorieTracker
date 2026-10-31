using System.Globalization;
using CalorieTracker.Identity.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CalorieTracker.Identity.Infrastructure.Tokens;

internal sealed class JwtAccessTokenIssuer(
    SigningKeyProvider signingKeys,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider)
    : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessToken Issue(UserAccount user)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now + jwt.AccessTokenLifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = signingKeys.SigningCredentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            },
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expiresAt);
    }
}
