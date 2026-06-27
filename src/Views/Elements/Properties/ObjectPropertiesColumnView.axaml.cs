using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;
using ClassicDiagnostics.Avalonia.Views;

namespace ClassicDiagnostics.Avalonia.Views.Elements.Properties;

internal partial class ObjectPropertiesColumnView : ReactiveUserControl<ObjectPropertiesColumnViewModel>
{
    public ObjectPropertiesColumnView()
    {
        InitializeComponent();
    }
}