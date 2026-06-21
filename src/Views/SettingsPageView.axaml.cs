using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class SettingsPageView : ReactiveUserControl<SettingsPageViewModel>
{
    public SettingsPageView(SettingsPageViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        LoadComponent();
    }
}