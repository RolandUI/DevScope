using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views.Elements.Properties;

internal partial class PropertyColumnsView : ReactiveUserControl<PropertyColumnsViewModel>
{
    public PropertyColumnsView()
    {
        InitializeComponent();
    }

    private void HandleColumnSplitterDragDelta(object? sender, VectorEventArgs args)
    {
        if (sender is Control { Tag: PropertyColumnViewModel column })
        {
            column.Width += args.Vector.X;
        }
    }
}