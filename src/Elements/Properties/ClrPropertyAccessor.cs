using System.Reflection;

namespace ClassicDiagnostics.Avalonia.Elements.Properties;

internal sealed class ClrPropertyAccessor : IPropertyAccessor
{
    public Type AssignedType => _assignedType;

    public Type? DeclaringType { get; }

    public string Group => "CLR Properties";

    public bool? IsAttached => null;

    public bool IsReadOnly => !Property.CanWrite;

    public object Key => Name;

    public string Name { get; }

    public string Priority => string.Empty;

    public PropertyInfo Property { get; }

    public Type PropertyType { get; }

    public object? Value => _value;

    private readonly object _target;
    private Type _assignedType;
    private object? _value;

    public ClrPropertyAccessor(object target, PropertyInfo property)
    {
        _target = target;
        Property = property;
        Name = property.DeclaringType is { IsInterface: true } ?
            property.DeclaringType.Name + '.' + property.Name :
            property.Name;
        DeclaringType = property.DeclaringType;
        PropertyType = property.PropertyType;
        _assignedType = property.PropertyType;
        Update();
    }

    public void Update()
    {
        object? value;
        Type? valueType = null;

        try
        {
            value = Property.GetValue(_target);
            valueType = value?.GetType();
        }
        catch (Exception exception)
        {
            value = exception.GetBaseException();
        }

        _value = value;
        _assignedType = valueType ?? Property.PropertyType;
    }

    public PropertyWriteResult Write(object? value)
    {
        try
        {
            Property.SetValue(_target, value);
            Update();
            return PropertyWriteResult.Success(value);
        }
        catch (Exception exception)
        {
            var message = $"Failed to set CLR property '{Property.DeclaringType?.Name}.{Property.Name}'.";
            DevToolsDiagnostics.Report(exception, message);
            return PropertyWriteResult.Failure(exception, message);
        }
    }
}