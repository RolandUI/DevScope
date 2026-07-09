using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Views;

internal partial class SettingsPageView : ReactiveUserControl<SettingsPageViewModel>
{
    public SettingsPageView(SettingsPageViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}