using ClassicDiagnostics.Avalonia.ViewModels;
using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal sealed class PropertyInspectionResult
{
    public IReadOnlyList<PropertyViewModel> Properties { get; }

    public IReadOnlyDictionary<object, PropertyViewModel[]> PropertyIndex { get; }

    public PropertyInspectionResult(
        IReadOnlyList<PropertyViewModel> properties,
        IReadOnlyDictionary<object, PropertyViewModel[]> propertyIndex)
    {
        Properties = properties;
        PropertyIndex = propertyIndex;
    }
}
