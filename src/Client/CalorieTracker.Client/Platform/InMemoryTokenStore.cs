namespace CalorieTracker.Client.Platform;

public sealed class InMemoryTokenStore : ITokenStore
{
    private StoredSession? _session;

    public Task<StoredSession?> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(_session);

    public Task SaveAsync(StoredSession session, CancellationToken cancellationToken)
    {
        _session = session;
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        _session = null;
        return Task.CompletedTask;
    }
}
