using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

internal sealed class PropertyColumnViewModel : ReactiveViewModelBase
{
    private const double DefaultColumnWidth = 360;
    private const double MinimumColumnWidth = 260;

    private readonly IPropertyColumnOwner _owner;
    private double _width = DefaultColumnWidth;

    public PropertyColumnViewModel(
        IPropertyColumnOwner owner,
        IPropertyColumnContentViewModel content)
    {
        _owner = owner;
        Content = content;
        Width = DefaultColumnWidth;
    }

    public IPropertyColumnContentViewModel Content { get; }

    internal PropertyColumnKey WidthKey => new(Content.Target.GetType(), Content.GetType());

    public string Title => Content.Title;

    public string Path => Content.Path;

    public bool CanClose
    {
        get;
        internal set => SetProperty(ref field, value);
    }

    public bool CanResize
    {
        get;
        internal set => SetProperty(ref field, value);
    }

    public double Width
    {
        get => _width;
        set
        {
            if (SetProperty(ref _width, Math.Max(MinimumColumnWidth, value)))
            {
                _owner.RememberWidth(this);
            }
        }
    }

    public void Close()
    {
        _owner.CloseFrom(this);
    }

    public void Refresh()
    {
        Content.Refresh();
    }

    internal void SetRememberedWidth(double width)
    {
        SetProperty(ref _width, Math.Max(MinimumColumnWidth, width), nameof(Width));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Content.Dispose();
        }

        base.Dispose(disposing);
    }
}

internal readonly record struct PropertyColumnKey(Type TargetType, Type ContentType);