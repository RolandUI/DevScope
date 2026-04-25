using Avalonia.Markup.Xaml;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class ControlDetailsView : UserControl
{
    private DataGrid _dataGrid;

    public ControlDetailsView()
    {
        InitializeComponent();

        _dataGrid = this.GetControl<DataGrid>("DataGrid");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void PropertiesGrid_OnDoubleTapped(object sender, TappedEventArgs e)
    {
        if (sender is DataGrid grid && grid.DataContext is ControlDetailsViewModel controlDetails)
        {
            controlDetails.NavigateToSelectedProperty();
        }
            
    }

    public void PropertyNamePressed(object sender, PointerPressedEventArgs e)
    {
        var mainVm = (ControlDetailsViewModel?) DataContext;

        if (mainVm is null)
        {
            return;
        }
            
        if (sender is Control control && control.DataContext is SetterViewModel setterVm)
        {
            mainVm.SelectProperty(setterVm.Property);

            if (mainVm.SelectedProperty is not null)
            {
                _dataGrid.ScrollIntoView(mainVm.SelectedProperty, null);
            }
        }
    }
}