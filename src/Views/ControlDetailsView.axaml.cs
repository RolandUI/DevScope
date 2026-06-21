using Avalonia.Markup.Xaml;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class ControlDetailsView : ReactiveUserControl<ControlDetailsViewModel>
{
    public ControlDetailsView(ControlDetailsViewModel viewModel) : base(viewModel)
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

    public void HandlePropertyNamePointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: SetterViewModel setterViewModel })
        {
            RequiredViewModel.SelectProperty(setterViewModel.Property);
            if (RequiredViewModel.SelectedProperty is not null)
            {
                DataGrid.ScrollIntoView(RequiredViewModel.SelectedProperty, null);
            }
        }
    }
}