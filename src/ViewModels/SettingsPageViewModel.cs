using RolandUI.DevScope.Shell;

namespace RolandUI.DevScope.ViewModels;

internal sealed class SettingsPageViewModel(MainViewModel mainViewModel) : ReactiveViewModelBase
{
    public MainViewModel MainView { get; } = mainViewModel;
}