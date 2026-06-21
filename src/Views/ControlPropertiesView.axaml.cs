using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class ControlPropertiesView : ReactiveUserControl<ControlDetailsViewModel>
{
    public ControlPropertiesView()
    {
        InitializeComponent();
    }

    public ControlPropertiesView(ControlDetailsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        LoadComponent();
    }

    private void HandlePropertiesGridDoubleTapped(object sender, TappedEventArgs e)
    {
        RequiredViewModel.NavigateToSelectedProperty();
    }
}