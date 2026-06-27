using ClassicDiagnostics.Avalonia.ViewModels;
using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal interface IPropertyEditorFactory
{
    PropertyEditorDescriptor Create(PropertyViewModel property);
}
