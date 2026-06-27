using ClassicDiagnostics.Avalonia.Shell;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal sealed class SettingsPageViewModel(MainViewModel mainViewModel) : ReactiveViewModelBase
{
    public MainViewModel MainView { get; } = mainViewModel;
}