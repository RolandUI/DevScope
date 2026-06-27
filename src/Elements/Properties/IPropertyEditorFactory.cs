using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties;

internal interface IPropertyEditorFactory
{
    PropertyEditorDescriptor Create(PropertyViewModel property);
}