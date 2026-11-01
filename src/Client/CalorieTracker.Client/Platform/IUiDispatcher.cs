using Avalonia.Threading;

namespace CalorieTracker.Client.Platform;

// UI state may only change on the UI thread; session events can arrive from HTTP threads
public interface IUiDispatcher
{
    void Post(Action action);
}

internal sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
