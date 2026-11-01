using System.Text.Json.Serialization;

namespace CalorieTracker.Identity.Infrastructure.Tokens;

// JWKS document (RFC 7517). Built field by field so a private key part can never leak into it.
public sealed record PublicJsonWebKeySet(
    [property: JsonPropertyName("keys")] IReadOnlyList<PublicJsonWebKey> Keys);

public sealed record PublicJsonWebKey(
    [property: JsonPropertyName("kty")] string KeyType,
    [property: JsonPropertyName("use")] string Use,
    [property: JsonPropertyName("alg")] string Algorithm,
    [property: JsonPropertyName("kid")] string KeyId,
    [property: JsonPropertyName("n")] string Modulus,
    [property: JsonPropertyName("e")] string Exponent);
