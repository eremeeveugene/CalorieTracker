using CalorieTracker.BuildingBlocks.Results;
using CalorieTracker.Client.Authentication;
using CalorieTracker.Client.Navigation;
using CalorieTracker.Client.Platform;
using CalorieTracker.Client.ViewModels;
using CalorieTracker.Contracts.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker.Client.UnitTests.ViewModels;

public sealed class ViewModelTests : IDisposable
{
    private readonly FakeAuthApi _api = new();
    private readonly AuthSession _session;
    private readonly RecordingNavigator _navigator = new();

    public ViewModelTests()
    {
        _session = new AuthSession(_api, new InMemoryTokenStore(), new FixedTimeProvider(TestClock.Now));
    }

    public void Dispose() => _session.Dispose();

    [Fact]
    public void Login_SignInCommand_RequiresEmailAndPassword()
    {
        var login = new LoginViewModel(_session, _navigator);
        login.SignInCommand.CanExecute(null).ShouldBeFalse();

        login.Email = "user@example.com";
        login.SignInCommand.CanExecute(null).ShouldBeFalse();

        login.Password = "password";
        login.SignInCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task Login_Rejected_ShowsServerMessage()
    {
        _api.LoginResult = Result.Failure<AuthTokensResponse>(FakeAuthApi.Rejected);
        var login = new LoginViewModel(_session, _navigator) { Email = "user@example.com", Password = "wrong" };

        await login.SignInCommand.ExecuteAsync(null);

        login.ErrorMessage.ShouldBe(FakeAuthApi.Rejected.Description);
        _session.IsSignedIn.ShouldBeFalse();
    }

    [Fact]
    public async Task Login_Success_SignsInAndClearsPassword()
    {
        var login = new LoginViewModel(_session, _navigator) { Email = " user@example.com ", Password = "password" };

        await login.SignInCommand.ExecuteAsync(null);

        _session.Email.ShouldBe("user@example.com");
        login.Password.ShouldBeEmpty();
        login.ErrorMessage.ShouldBeNull();
    }

    [Theory]
    [InlineData("short", "short", false)]
    [InlineData("long-enough-password", "different-password", false)]
    [InlineData("long-enough-password", "long-enough-password", true)]
    public void Register_RequiresLongMatchingPasswords(string password, string confirmation, bool canRegister)
    {
        var register = new RegisterViewModel(_session, _navigator)
        {
            Email = "user@example.com",
            Password = password,
            ConfirmPassword = confirmation,
        };

        register.RegisterCommand.CanExecute(null).ShouldBe(canRegister);
        (register.PasswordHint is null).ShouldBe(canRegister);
    }

    [Fact]
    public async Task Shell_FollowsTheSession()
    {
        await using var services = new ServiceCollection()
            .AddSingleton(_session)
            .AddSingleton<Navigator>()
            .AddSingleton<INavigator>(provider => provider.GetRequiredService<Navigator>())
            .AddTransient<LoginViewModel>()
            .AddTransient<RegisterViewModel>()
            .AddTransient<HomeViewModel>()
            .BuildServiceProvider();
        var navigator = services.GetRequiredService<Navigator>();
        using var shell = new MainViewModel(navigator, _session, new ImmediateUiDispatcher());

        await shell.StartAsync(CancellationToken.None);
        navigator.CurrentPage.ShouldBeOfType<LoginViewModel>();

        await _session.SignInAsync("user@example.com", "password", CancellationToken.None);
        navigator.CurrentPage.ShouldBeOfType<HomeViewModel>();

        await _session.SignOutAsync(CancellationToken.None);
        navigator.CurrentPage.ShouldBeOfType<LoginViewModel>();
    }
}
