using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class TracePageView : ReactiveUserControl<TracePageViewModel>
{
    public TracePageView(TracePageViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        LoadComponent();
    }
}