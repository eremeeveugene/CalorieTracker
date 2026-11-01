using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using CalorieTracker.Client.Platform;

namespace CalorieTracker.Client.Desktop;

// Encrypts the session with Windows DPAPI: only the same Windows user on the same machine can read it
[SupportedOSPlatform("windows")]
internal sealed class WindowsProtectedTokenStore(string filePath) : ITokenStore
{
    private static readonly byte[] Entropy = "CalorieTracker.Session"u8.ToArray();

    public async Task<StoredSession?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var encrypted = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var json = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<StoredSession>(json);
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            // Unreadable (another user, another machine or corrupted): sign in again
            return null;
        }
    }

    public async Task SaveAsync(StoredSession session, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var json = JsonSerializer.SerializeToUtf8Bytes(session);
        var encrypted = ProtectedData.Protect(json, Entropy, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(filePath, encrypted, cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        File.Delete(filePath);
        return Task.CompletedTask;
    }
}
