namespace RolandUI.DevScope.Elements.Properties;

internal sealed class PropertyValueChildDescriptor(
    string name,
    Type valueType,
    Func<object?> read,
    Func<object?, PropertyWriteResult>? write = null)
{
    public string Name { get; } = name;

    public object? Value => read();

    public Type ValueType { get; } = valueType;

    public bool IsReadOnly => write is null;

    public bool CanNavigate => PropertyValueDescriptorFactory.Create(Value).CanNavigate;

    public PropertyWriteResult Write(object? value)
    {
        if (write is not null)
        {
            return write(value);
        }

        var exception = new InvalidOperationException($"Collection item '{Name}' is read-only.");
        return PropertyWriteResult.Failure(exception, exception.Message);
    }
}
