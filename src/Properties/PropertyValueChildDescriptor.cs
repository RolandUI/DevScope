using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal sealed class PropertyValueChildDescriptor
{
    public string Name { get; }

    public object? Value { get; }

    public Type ValueType { get; }

    public bool IsReadOnly { get; }

    public bool CanNavigate { get; }

    public PropertyValueChildDescriptor(string name, object? value, bool isReadOnly = true)
    {
        Name = name;
        Value = value;
        ValueType = value?.GetType() ?? typeof(object);
        IsReadOnly = isReadOnly;
        CanNavigate = PropertyValueDescriptorFactory.Default.Create(value).CanNavigate;
    }
}
