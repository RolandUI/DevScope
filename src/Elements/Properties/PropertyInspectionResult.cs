using RolandUI.DevScope.Elements.Properties.ViewModels;

namespace RolandUI.DevScope.Elements.Properties;

internal sealed class PropertyInspectionResult(
    IReadOnlyList<PropertyViewModel> properties,
    IReadOnlyDictionary<object, PropertyViewModel[]> propertyIndex
)
{
    public IReadOnlyList<PropertyViewModel> Properties { get; } = properties;

    public IReadOnlyDictionary<object, PropertyViewModel[]> PropertyIndex { get; } = propertyIndex;
}