using CalorieTracker.Client.Authentication;
using CalorieTracker.Client.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalorieTracker.Client.ViewModels;

public sealed partial class LoginViewModel(AuthSession session, INavigator navigator) : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    // The shell switches to the signed-in pages when the session signs in
    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = await session.SignInAsync(Email.Trim(), Password, cancellationToken);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Description;
            return;
        }

        Password = string.Empty;
    }

    private bool CanSignIn() => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(Password);

    [RelayCommand]
    private void GoToRegister() => navigator.NavigateTo<RegisterViewModel>();
}
