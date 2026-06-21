using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassicDiagnostics.Avalonia.ValueConverters;

internal class EnumToCheckedConverter : IValueConverter
{
    public static EnumToCheckedConverter Shared { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Equals(value, parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? parameter : BindingOperations.DoNothing;
    }
}
