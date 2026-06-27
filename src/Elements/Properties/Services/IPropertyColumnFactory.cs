using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.Services;

internal interface IPropertyColumnFactory
{
    PropertyColumnViewModel CreateColumn(
        IPropertyColumnOwner owner,
        object target,
        string title,
        string path);
}