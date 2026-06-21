using ClassicDiagnostics.Avalonia.ViewModels;

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
