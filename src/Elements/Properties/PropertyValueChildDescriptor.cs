namespace RolandUI.DevScope.Elements.Properties;

internal sealed class PropertyValueChildDescriptor(string name, object? value, bool isReadOnly = true)
{
    public string Name { get; } = name;

    public object? Value { get; } = value;

    public Type ValueType { get; } = value?.GetType() ?? typeof(object);

    public bool IsReadOnly { get; } = isReadOnly;

    public bool CanNavigate { get; } = PropertyValueDescriptorFactory.Create(value).CanNavigate;
}