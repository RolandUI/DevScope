using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class HotKeyPageView : ReactiveUserControl<HotKeyPageViewModel>
{
    public HotKeyPageView(HotKeyPageViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}