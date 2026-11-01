using CalorieTracker.Client.Authentication;
using CommunityToolkit.Mvvm.Input;

namespace CalorieTracker.Client.ViewModels;

public sealed partial class HomeViewModel(AuthSession session) : ViewModelBase
{
    public string Email => session.Email ?? string.Empty;

    [RelayCommand]
    private Task SignOutAsync(CancellationToken cancellationToken) => session.SignOutAsync(cancellationToken);
}
