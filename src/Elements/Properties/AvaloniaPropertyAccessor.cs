namespace RolandUI.DevScope.Elements.Properties;

internal sealed class AvaloniaPropertyAccessor : IPropertyAccessor
{
    public Type AssignedType => _assignedType;

    public Type? DeclaringType { get; }

    public string Group => _group;

    public bool? IsAttached => Property.IsAttached;

    public bool IsReadOnly => Property.IsReadOnly;

    public object Key => Property;

    public string Name { get; }

    public string Priority => _priority;

    public AvaloniaProperty Property { get; }

    public Type PropertyType { get; }

    public object? Value => _value;

    private readonly AvaloniaObject _target;
    private Type _assignedType;
    private string _group;
    private string _priority;
    private object? _value;

    public AvaloniaPropertyAccessor(AvaloniaObject target, AvaloniaProperty property)
    {
        _target = target;
        Property = property;
        Name = property.IsAttached ? $"[{property.OwnerType.Name}.{property.Name}]" : property.Name;
        DeclaringType = property.OwnerType;
        PropertyType = property.PropertyType;
        _assignedType = property.PropertyType;
        _group = property.IsAttached ? "Attached Properties" : "Properties";
        _priority = "Unset";
        Update();
    }

    public void Update()
    {
        if (Property.IsDirect)
        {
            UpdateDirectProperty();
            return;
        }

        UpdateStyledProperty();
    }

    public PropertyWriteResult Write(object? value)
    {
        try
        {
            _target.SetValue(Property, value);
            Update();
            return PropertyWriteResult.Success(value);
        }
        catch (Exception exception)
        {
            var message = $"Failed to set Avalonia property '{Property.OwnerType.Name}.{Property.Name}'.";
            DevToolsDiagnostics.Report(exception, message);
            return PropertyWriteResult.Failure(exception, message);
        }
    }

    private void UpdateDirectProperty()
    {
        object? value;
        Type? valueType = null;

        try
        {
            value = _target.GetValue(Property);
            valueType = value?.GetType();
        }
        catch (Exception exception)
        {
            value = exception.GetBaseException();
        }

        _value = value;
        _assignedType = valueType ?? Property.PropertyType;
        _priority = "Direct";
        _group = "Properties";
    }

    private void UpdateStyledProperty()
    {
        object? value;
        Type? valueType = null;
        BindingPriority? priority = null;

        try
        {
            var diagnostic = _target.GetDiagnostic(Property);
            value = diagnostic.Value;
            valueType = value?.GetType();
            priority = diagnostic.Priority;
        }
        catch (Exception exception)
        {
            value = exception.GetBaseException();
        }

        _value = value;
        _assignedType = valueType ?? Property.PropertyType;

        if (priority is not null)
        {
            _priority = priority.ToString()!;
            _group = IsAttached == true ? "Attached Properties" : "Properties";
        }
        else
        {
            _priority = "Unset";
            _group = "Unset";
        }
    }
}