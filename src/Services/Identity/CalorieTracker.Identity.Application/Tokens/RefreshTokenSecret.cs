using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace CalorieTracker.Identity.Application.Tokens;

internal static class RefreshTokenSecret
{
    private const int SizeInBytes = 32;

    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SizeInBytes));

    // Tokens are 256-bit random values, so an unsalted fast hash is enough to protect them at rest
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
