using RolandUI.DevScope.Elements.Properties.Services;
using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Elements.Properties.ViewModels;

internal sealed class PropertyContainerItemViewModel(
    PropertyValueChildDescriptor descriptor,
    object? dictionaryKey,
    int index,
    Type declaringType
) : PropertyViewModel, IPropertyColumnItemViewModel
{
    public override Type AssignedType => Value?.GetType() ?? descriptor.ValueType;

    public override Type? DeclaringType { get; } = declaringType;

    public override string Group => dictionaryKey is null ? "Items" : "Entries";

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
            var result = descriptor.Write(value);
            ValueError = result.ErrorMessage;

            if (result.IsSuccess)
            {
                RaisePropertyStateChanged();
            }
        }
    }

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

    public int Index { get; } = index;

    public Type ValueType => AssignedType;

    public bool CanNavigate => PropertyColumnNavigation.CanNavigate(Value);

    public string KeyText => dictionaryKey?.ToString() ?? string.Empty;

    public string ValueText => Value?.ToString() ?? "(null)";

    public override void Update()
    {
        RaisePropertyStateChanged();
    }

    private void RaisePropertyStateChanged()
    {
        RaisePropertyChanged(nameof(Value));
        RaisePropertyChanged(nameof(AssignedType));
        RaisePropertyChanged(nameof(ValueText));
        RaisePropertyChanged(nameof(ValueType));
        RaisePropertyChanged(nameof(Type));
        RaisePropertyChanged(nameof(TypeTooltip));
        RaisePropertyChanged(nameof(CanNavigate));
        RaisePropertyChanged(nameof(AssignedTypeTooltip));
        RaisePropertyChanged(nameof(PropertyTypeTooltip));
    }
}
