using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassicDiagnostics.Avalonia.ValueConverters;

internal class BoolToOpacityConverter : IValueConverter
{
    public static BoolToOpacityConverter Shared { get; } = new();

    public double Opacity { get; set; } = 0.6;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 1d : Opacity;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
