using CalorieTracker.Client.Authentication;
using CalorieTracker.Client.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalorieTracker.Client.ViewModels;

public sealed partial class RegisterViewModel(AuthSession session, INavigator navigator) : ViewModelBase
{
    // Mirrors Identity's password policy for instant feedback; the server stays authoritative
    public const int MinimumPasswordLength = 10;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordHint))]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordHint))]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public string? PasswordHint
    {
        get
        {
            if (Password.Length < MinimumPasswordLength)
            {
                return $"Use at least {MinimumPasswordLength} characters.";
            }

            return ConfirmPassword.Length > 0 && Password != ConfirmPassword ? "The passwords don't match." : null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = await session.RegisterAsync(Email.Trim(), Password, cancellationToken);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Description;
        }
    }

    private bool CanRegister() =>
        !string.IsNullOrWhiteSpace(Email)
        && Password.Length >= MinimumPasswordLength
        && Password == ConfirmPassword;

    [RelayCommand]
    private void GoToLogin() => navigator.NavigateTo<LoginViewModel>();
}
