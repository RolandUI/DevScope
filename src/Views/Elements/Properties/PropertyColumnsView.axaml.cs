using RolandUI.DevScope.Elements.Properties.ViewModels;

namespace RolandUI.DevScope.Views.Elements.Properties;

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