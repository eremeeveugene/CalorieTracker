using CalorieTracker.Client.Platform;

namespace CalorieTracker.Client.Desktop;

internal sealed class DesktopPlatformServices : IPlatformServices
{
    public ITokenStore CreateTokenStore()
    {
        if (!OperatingSystem.IsWindows())
        {
            // macOS Keychain and Linux Secret Service come later; until then nothing is written to disk
            return new InMemoryTokenStore();
        }

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalorieTracker");
        return new WindowsProtectedTokenStore(Path.Combine(directory, "session.bin"));
    }
}
