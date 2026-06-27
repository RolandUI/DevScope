using ClassicDiagnostics.Avalonia.Elements.Properties.Services;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

internal sealed class PropertyColumnPropertyItemViewModel(PropertyViewModel property) : IPropertyColumnItemViewModel
{
    public PropertyViewModel Property { get; } = property;

    public string Name => Property.Name;

    public object? Value => Property.Value;

    public bool CanNavigate => IsNavigableObject(Value);

    internal static bool IsNavigableObject(object? value)
    {
        return value is not (null or string) && PropertyColumnNavigation.CanNavigate(value);
    }
}