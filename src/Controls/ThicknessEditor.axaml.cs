using Avalonia.Media;

namespace ClassicDiagnostics.Avalonia.Controls;

internal class ThicknessEditor : ContentControl
{
    public readonly static StyledProperty<Thickness> ThicknessProperty =
        AvaloniaProperty.Register<ThicknessEditor, Thickness>(
            nameof(Thickness),
            defaultBindingMode: BindingMode.TwoWay);

    public readonly static StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<ThicknessEditor, string?>(nameof(Header));

    public readonly static StyledProperty<bool> IsPresentProperty =
        AvaloniaProperty.Register<ThicknessEditor, bool>(nameof(IsPresent), true);

    public readonly static StyledProperty<double> LeftProperty =
        AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Left));

    public readonly static StyledProperty<double> TopProperty =
        AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Top));

    public readonly static StyledProperty<double> RightProperty =
        AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Right));

    public readonly static StyledProperty<double> BottomProperty =
        AvaloniaProperty.Register<ThicknessEditor, double>(nameof(Bottom));

    public readonly static StyledProperty<IBrush> HighlightProperty =
        AvaloniaProperty.Register<ThicknessEditor, IBrush>(nameof(Highlight));

    private bool _isUpdatingThickness;

    public Thickness Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public bool IsPresent
    {
        get => GetValue(IsPresentProperty);
        set => SetValue(IsPresentProperty, value);
    }

    public double Left
    {
        get => GetValue(LeftProperty);
        set => SetValue(LeftProperty, value);
    }

    public double Top
    {
        get => GetValue(TopProperty);
        set => SetValue(TopProperty, value);
    }

    public double Right
    {
        get => GetValue(RightProperty);
        set => SetValue(RightProperty, value);
    }

    public double Bottom
    {
        get => GetValue(BottomProperty);
        set => SetValue(BottomProperty, value);
    }

    public IBrush Highlight
    {
        get => GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ThicknessProperty)
        {
            try
            {
                _isUpdatingThickness = true;

                var value = change.GetNewValue<Thickness>();

                SetCurrentValue(LeftProperty, value.Left);
                SetCurrentValue(TopProperty, value.Top);
                SetCurrentValue(RightProperty, value.Right);
                SetCurrentValue(BottomProperty, value.Bottom);
            }
            finally
            {
                _isUpdatingThickness = false;
            }
        }
        else if (!_isUpdatingThickness && change.Property.Name is nameof(Left) or nameof(Top) or nameof(Right) or nameof(Bottom))
        {
            SetCurrentValue(ThicknessProperty, new Thickness(Left, Top, Right, Bottom));
        }
    }
}