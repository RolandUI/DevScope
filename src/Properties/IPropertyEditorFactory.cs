using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal interface IPropertyEditorFactory
{
    PropertyEditorDescriptor Create(PropertyViewModel property);
}
