using CalorieTracker.Client.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker.Client.Navigation;

public interface INavigator
{
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
}

// Shows one page at a time; the ViewLocator picks the view for the current page's view model
public sealed partial class Navigator(IServiceProvider services) : ObservableObject, INavigator
{
    [ObservableProperty]
    public partial ViewModelBase? CurrentPage { get; private set; }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase =>
        CurrentPage = services.GetRequiredService<TViewModel>();
}
