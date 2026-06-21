using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ClassicDiagnostics.Avalonia.ValueConverters;

internal class BrushSelectorConverter : AvaloniaObject, IValueConverter
{
    public readonly static DirectProperty<BrushSelectorConverter, IBrush?> BrushProperty =
        AvaloniaProperty.RegisterDirect<BrushSelectorConverter, IBrush?>(nameof(Brush), o => o.Brush, (o, v) => o.Brush = v);

    public static BrushSelectorConverter Shared { get; } = new();

    public IBrush? Brush { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (ReferenceEquals(value, parameter) ||
            value is ISolidColorBrush a
            && parameter is ISolidColorBrush b
            && a.Color == b.Color
            && a.Transform == b.Transform
            && b.Opacity == a.Opacity)
        {
            return Brush;
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
