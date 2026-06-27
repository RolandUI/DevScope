using System.ComponentModel;
using ClassicDiagnostics.Avalonia.Properties;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

internal sealed class AvaloniaPropertyViewModel(AvaloniaObject target, AvaloniaProperty property) : PropertyViewModel
{
    public AvaloniaProperty Property => _accessor.Property;

    public override Type AssignedType => _accessor.AssignedType;

    public override Type? DeclaringType => _accessor.DeclaringType;

    public override string Group => IsPinned ? "Pinned" : _accessor.Group;

    public override bool? IsAttached => _accessor.IsAttached;

    public override bool IsReadonly => _accessor.IsReadOnly;

    public override object Key => _accessor.Key;

    public override string Name => _accessor.Name;

    public override string Priority => _accessor.Priority;

    public override Type PropertyType => _accessor.PropertyType;

    public override object? Value
    {
        get => _accessor.Value;
        set
        {
            if (_accessor.Write(value).IsSuccess)
            {
                RaisePropertyStateChanged();
            }
        }
    }

    private readonly AvaloniaPropertyAccessor _accessor = new(target, property);

    public override void Update()
    {
        _accessor.Update();
        RaisePropertyStateChanged();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsPinned))
        {
            RaisePropertyChanged(nameof(Group));
        }
    }

    private void RaisePropertyStateChanged()
    {
        RaisePropertyChanged(nameof(Value));
        RaisePropertyChanged(nameof(AssignedType));
        RaisePropertyChanged(nameof(Priority));
        RaisePropertyChanged(nameof(Group));
        RaisePropertyChanged(nameof(Type));
        RaisePropertyChanged(nameof(TypeTooltip));
        RaisePropertyChanged(nameof(AssignedTypeTooltip));
    }
}