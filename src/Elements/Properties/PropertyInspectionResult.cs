using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties;

internal sealed class PropertyInspectionResult(
    IReadOnlyList<PropertyViewModel> properties,
    IReadOnlyDictionary<object, PropertyViewModel[]> propertyIndex
)
{
    public IReadOnlyList<PropertyViewModel> Properties { get; } = properties;

    public IReadOnlyDictionary<object, PropertyViewModel[]> PropertyIndex { get; } = propertyIndex;
}