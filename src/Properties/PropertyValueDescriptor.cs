using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal sealed class PropertyValueDescriptor
{
    public PropertyValueDescriptorKind Kind { get; }

    public Type ValueType { get; }

    public int? Count { get; }

    public bool IsReadOnly { get; }

    public bool CanNavigate { get; }

    public IReadOnlyList<PropertyValueChildDescriptor> Children { get; }

    public PropertyValueDescriptor(
        PropertyValueDescriptorKind kind,
        Type valueType,
        int? count,
        bool isReadOnly,
        bool canNavigate,
        IReadOnlyList<PropertyValueChildDescriptor> children)
    {
        Kind = kind;
        ValueType = valueType;
        Count = count;
        IsReadOnly = isReadOnly;
        CanNavigate = canNavigate;
        Children = children;
    }
}
