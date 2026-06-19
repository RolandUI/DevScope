using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassicDiagnostics.Avalonia.Converters;

internal class BoolToOpacityConverter : IValueConverter
{
    public double Opacity { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 1d : Opacity;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}