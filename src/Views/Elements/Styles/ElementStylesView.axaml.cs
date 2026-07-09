using RolandUI.DevScope.Elements;
using RolandUI.DevScope.Elements.Styles;

namespace RolandUI.DevScope.Views.Elements.Styles;

internal partial class ElementStylesView : ReactiveUserControl<ElementDetailsViewModel>
{
    public ElementStylesView()
    {
        InitializeComponent();
    }

    public void HandlePropertyNamePointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: SetterViewModel setterViewModel })
        {
            RequiredViewModel.SelectProperty(setterViewModel.Property);
        }
    }
}