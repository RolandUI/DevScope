using System.ComponentModel;
using System.Reflection;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal sealed class ClrPropertyViewModel : PropertyViewModel
{
    private readonly object _target;
    private Type _assignedType;
    private object? _value;

    public ClrPropertyViewModel(object o, PropertyInfo property)
    {
        _target = o;
        Property = property;

        if (property.DeclaringType is not { IsInterface: true })
        {
            Name = property.Name;
        }
        else
        {
            Name = property.DeclaringType.Name + '.' + property.Name;
        }

        DeclaringType = property.DeclaringType;
        PropertyType = property.PropertyType;
        _assignedType = property.PropertyType;

        Update();
    }

    public PropertyInfo Property { get; }
    public override object Key => Name;
    public override string Name { get; }
    public override string Group => IsPinned ? "Pinned" : "CLR Properties";

    public override Type PropertyType { get; }
    public override Type AssignedType => _assignedType;

    public override bool IsReadonly => !Property.CanWrite;

    public override object? Value
    {
        get => _value;
        set
        {
            try
            {
                Property.SetValue(_target, value);
                Update();
            }
            catch (Exception exception)
            {
                DevToolsDiagnostics.Report(
                    exception,
                    $"Failed to set CLR property '{Property.DeclaringType?.Name}.{Property.Name}'.");
            }
        }
    }

    public override string Priority => string.Empty;

    public override bool? IsAttached => null;

    public override Type? DeclaringType { get; }

    public override void Update()
    {
        object? value;
        Type? valueType = null;

        try
        {
            value = Property.GetValue(_target);
            valueType = value?.GetType();
        }
        catch (Exception e)
        {
            value = e.GetBaseException();
        }

        SetProperty(ref _value, value, nameof(Value));
        SetProperty(ref _assignedType, valueType ?? Property.PropertyType, nameof(AssignedType));
        RaisePropertyChanged(nameof(Type));
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsPinned))
        {
            RaisePropertyChanged(nameof(Group));
        }
    }
}
