using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassicDiagnostics.Avalonia.Converters;

internal class EnumToCheckedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? parameter : BindingOperations.DoNothing;
    }
}