using CalorieTracker.Client.Authentication;
using CalorieTracker.Client.Navigation;
using CalorieTracker.Client.Platform;

namespace CalorieTracker.Client.ViewModels;

// Shell of the app: shows the sign-in pages or the signed-in pages, following the session
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly AuthSession _session;
    private readonly IUiDispatcher _dispatcher;

    public MainViewModel(Navigator navigator, AuthSession session, IUiDispatcher dispatcher)
    {
        Navigator = navigator;
        _session = session;
        _dispatcher = dispatcher;
        _session.SignedInChanged += OnSignedInChanged;
    }

    public Navigator Navigator { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _session.RestoreAsync(cancellationToken);
        ShowPageForSession();
    }

    public void Dispose() => _session.SignedInChanged -= OnSignedInChanged;

    private void OnSignedInChanged(object? sender, EventArgs e) => _dispatcher.Post(ShowPageForSession);

    private void ShowPageForSession()
    {
        if (_session.IsSignedIn)
        {
            Navigator.NavigateTo<HomeViewModel>();
        }
        else
        {
            Navigator.NavigateTo<LoginViewModel>();
        }
    }
}
