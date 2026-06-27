using ClassicDiagnostics.Avalonia.Elements;
using ClassicDiagnostics.Avalonia.Elements.Styles;

namespace ClassicDiagnostics.Avalonia.Views.Elements.Styles;

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