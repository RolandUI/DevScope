using ClassicDiagnostics.Avalonia.Properties;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal sealed class PropertyValueChildViewModel : PropertyViewModel
{
    public override Type AssignedType => _descriptor.ValueType;

    public override Type? DeclaringType { get; }

    public override string Group { get; }

    public override bool? IsAttached => null;

    public override bool IsReadonly => _descriptor.IsReadOnly;

    public override object Key => Name;

    public override string Name => _descriptor.Name;

    public override string Priority => string.Empty;

    public override Type PropertyType => _descriptor.ValueType;

    public override object? Value
    {
        get => _descriptor.Value;
        set
        {
        }
    }

    private readonly PropertyValueChildDescriptor _descriptor;

    public PropertyValueChildViewModel(
        PropertyValueChildDescriptor descriptor,
        Type? declaringType,
        string group)
    {
        _descriptor = descriptor;
        DeclaringType = declaringType;
        Group = group;
    }

    public override void Update()
    {
    }
}
