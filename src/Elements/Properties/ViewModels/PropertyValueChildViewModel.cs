namespace ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

internal sealed class PropertyValueChildViewModel(
    PropertyValueChildDescriptor descriptor,
    Type? declaringType,
    string group
) : PropertyViewModel
{
    public override Type AssignedType => descriptor.ValueType;

    public override Type? DeclaringType { get; } = declaringType;

    public override string Group { get; } = group;

    public override bool? IsAttached => null;

    public override bool IsReadonly => descriptor.IsReadOnly;

    public override object Key => Name;

    public override string Name => descriptor.Name;

    public override string Priority => string.Empty;

    public override Type PropertyType => descriptor.ValueType;

    public override object? Value
    {
        get => descriptor.Value;
        set
        {
        }
    }

    public override void Update()
    {
    }
}