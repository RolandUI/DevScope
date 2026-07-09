namespace RolandUI.DevScope.Elements.Properties.ViewModels;

internal sealed class PropertyValueChildViewModel(
    PropertyValueChildDescriptor descriptor,
    Type? declaringType,
    string group
) : PropertyViewModel
{
    public override Type AssignedType => Value?.GetType() ?? descriptor.ValueType;

    public override Type? DeclaringType { get; } = declaringType;

    public override string Group { get; } = group;

    public override bool? IsAttached => null;

    public override bool IsReadonly => descriptor.IsReadOnly;

    public override object Key => Name;

    public override string Name => descriptor.Name;

    public override string Priority => string.Empty;

    public override Type PropertyType => descriptor.ValueType;

    public override string? ValueError
    {
        get;
        protected set
        {
            if (SetProperty(ref field, value))
            {
                RaisePropertyChanged(nameof(Value));
            }
        }
    }

    public override object? Value
    {
        get => descriptor.Value;
        set
        {
            var result = descriptor.Write(value);
            ValueError = result.ErrorMessage;

            if (result.IsSuccess)
            {
                RaisePropertyStateChanged();
            }
        }
    }

    public override void Update()
    {
        RaisePropertyStateChanged();
    }

    private void RaisePropertyStateChanged()
    {
        RaisePropertyChanged(nameof(Value));
        RaisePropertyChanged(nameof(AssignedType));
        RaisePropertyChanged(nameof(Type));
        RaisePropertyChanged(nameof(TypeTooltip));
        RaisePropertyChanged(nameof(AssignedTypeTooltip));
    }
}
