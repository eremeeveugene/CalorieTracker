namespace CalorieTracker.Client.Platform;

// Keeps the refresh token between app launches; implementations must store it securely
public interface ITokenStore
{
    Task<StoredSession?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(StoredSession session, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}

public sealed record StoredSession(string Email, string RefreshToken)
{
    public override string ToString() => $"{nameof(StoredSession)} {{ Email = {Email} }}";
}
