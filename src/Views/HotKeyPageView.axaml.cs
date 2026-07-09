using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Views;

internal partial class HotKeyPageView : ReactiveUserControl<HotKeyPageViewModel>
{
    public HotKeyPageView(HotKeyPageViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}