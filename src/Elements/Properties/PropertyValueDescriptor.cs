namespace RolandUI.DevScope.Elements.Properties;

internal sealed class PropertyValueDescriptor(
    PropertyValueDescriptorKind kind,
    Type valueType,
    int? count,
    bool isReadOnly,
    bool canNavigate,
    IReadOnlyList<PropertyValueChildDescriptor> children
)
{
    public PropertyValueDescriptorKind Kind { get; } = kind;

    public Type ValueType { get; } = valueType;

    public int? Count { get; } = count;

    public bool IsReadOnly { get; } = isReadOnly;

    public bool CanNavigate { get; } = canNavigate;

    public IReadOnlyList<PropertyValueChildDescriptor> Children { get; } = children;
}