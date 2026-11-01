using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CalorieTracker.Client.Platform;
using CalorieTracker.Client.ViewModels;
using CalorieTracker.Client.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker.Client;

public partial class App : Application
{
    private readonly IPlatformServices _platformServices;

    // Used by heads that construct App themselves (Android, iOS, Browser) and by the previewer
    public App()
        : this(new DefaultPlatformServices())
    {
    }

    public App(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection()
            .AddClientServices(_platformServices, ClientSettings.FromEnvironment())
            .BuildServiceProvider();
        var mainViewModel = services.GetRequiredService<MainViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = mainViewModel };
            desktop.Exit += (_, _) => services.Dispose();
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = mainViewModel };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView { DataContext = mainViewModel };
        }

        _ = mainViewModel.StartAsync(CancellationToken.None);

        base.OnFrameworkInitializationCompleted();
    }
}
