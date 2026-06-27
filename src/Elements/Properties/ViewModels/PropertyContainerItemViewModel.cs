using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

internal sealed class PropertyContainerItemViewModel(
    string name,
    object? key,
    object? value,
    int index
) : ViewModelBase, IPropertyColumnItemViewModel
{

    public string Name { get; } = name;

    public object? Key { get; } = key;

    public object? Value { get; } = value;

    public int Index { get; } = index;

    public Type ValueType { get; } = value?.GetType() ?? typeof(object);

    public bool CanNavigate => PropertyColumnNavigation.CanNavigate(Value);

    public string KeyText => Key?.ToString() ?? string.Empty;

    public string ValueText => Value?.ToString() ?? "(null)";

    public string Type => ValueType.GetTypeName();

    public string TypeTooltip => ValueType.GetDetailedTypeName();
}