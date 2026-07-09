using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Elements.Properties.ViewModels;

internal abstract class PropertyViewModel : ViewModelBase
{
    public abstract object Key { get; }
    public abstract string Name { get; }
    public abstract string Group { get; }
    public abstract Type AssignedType { get; }
    public abstract Type? DeclaringType { get; }
    public abstract object? Value { get; set; }
    public virtual string? ValueError { get; protected set; }
    public abstract string Priority { get; }
    public abstract bool? IsAttached { get; }
    public abstract Type PropertyType { get; }

    public string Type => PropertyType == AssignedType ?
        PropertyType.GetTypeName() :
        $"{PropertyType.GetTypeName()} {{{AssignedType.GetTypeName()}}}";

    public string TypeTooltip => PropertyType == AssignedType ?
        PropertyType.GetDetailedTypeName() :
        $"Property type:{Environment.NewLine}{PropertyType.GetDetailedTypeName()}{Environment.NewLine}{Environment.NewLine}Assigned type:{Environment.NewLine}{AssignedType.GetDetailedTypeName()}";

    public string AssignedTypeTooltip => AssignedType.GetDetailedTypeName();

    public string PropertyTypeTooltip => PropertyType.GetDetailedTypeName();

    public abstract bool IsReadonly { get; }

    public bool IsPinned
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string FullName => $"{GetType().Name.Replace("PropertyViewModel", "")}:{DeclaringType?.FullName}.{Name}";
    public abstract void Update();
}
