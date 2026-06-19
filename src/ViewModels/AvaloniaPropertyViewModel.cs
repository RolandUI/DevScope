using System.ComponentModel;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal sealed class AvaloniaPropertyViewModel : PropertyViewModel
{
    private readonly AvaloniaObject _target;
    private Type _assignedType;
    private string _group;
    private string _priority;
    private object? _value;

    public AvaloniaPropertyViewModel(AvaloniaObject o, AvaloniaProperty property)
    {
        _target = o;
        Property = property;

        Name = property.IsAttached ?
            $"[{property.OwnerType.Name}.{property.Name}]" :
            property.Name;
        DeclaringType = property.OwnerType;
        PropertyType = property.PropertyType;
        _assignedType = property.PropertyType;
        _group = property.IsAttached ? "Attached Properties" : "Properties";
        _priority = "Unset";
        Update();
    }

    public AvaloniaProperty Property { get; }
    public override object Key => Property;
    public override string Name { get; }
    public override bool? IsAttached => Property.IsAttached;
    public override string Priority => _priority;
    public override Type AssignedType => _assignedType;

    public override object? Value
    {
        get => _value;
        set
        {
            try
            {
                _target.SetValue(Property, value);
                Update();
            }
            catch (Exception exception)
            {
                DevToolsDiagnostics.Report(
                    exception,
                    $"Failed to set Avalonia property '{Property.OwnerType.Name}.{Property.Name}'.");
            }
        }
    }

    public override string Group => IsPinned ? "Pinned" : _group;

    public override Type? DeclaringType { get; }
    public override Type PropertyType { get; }
    public override bool IsReadonly => Property.IsReadOnly;

    public override void Update()
    {
        if (Property.IsDirect)
        {
            object? value;
            Type? valueType = null;

            try
            {
                value = _target.GetValue(Property);
                valueType = value?.GetType();
            }
            catch (Exception e)
            {
                value = e.GetBaseException();
            }

            SetProperty(ref _value, value, nameof(Value));
            SetProperty(ref _assignedType, valueType ?? Property.PropertyType, nameof(AssignedType));
            SetProperty(ref _priority, "Direct", nameof(Priority));

            _group = "Properties";
        }
        else
        {
            object? value;
            Type? valueType = null;
            BindingPriority? priority = null;

            try
            {
                var diag = AvaloniaPrivateApi.Current.GetDiagnosticValue(_target, Property);

                value = diag.Value;
                valueType = value?.GetType();
                priority = diag.Priority;
            }
            catch (Exception e)
            {
                value = e.GetBaseException();
            }

            SetProperty(ref _value, value, nameof(Value));
            SetProperty(ref _assignedType, valueType ?? Property.PropertyType, nameof(AssignedType));

            if (priority != null)
            {
                SetProperty(ref _priority, priority.ToString()!, nameof(Priority));
                SetProperty(ref _group, IsAttached == true ? "Attached Properties" : "Properties", nameof(Group));
            }
            else
            {
                SetProperty(ref _priority, "Unset", nameof(Priority));
                SetProperty(ref _group, "Unset", nameof(Group));
            }
        }
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
