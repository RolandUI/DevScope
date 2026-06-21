using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassicDiagnostics.Avalonia.ValueConverters;

internal class GetTypeNameConverter : IValueConverter
{
    public static GetTypeNameConverter Shared { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Type type)
        {
            return type.GetTypeName();
        }

        return BindingOperations.DoNothing;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
