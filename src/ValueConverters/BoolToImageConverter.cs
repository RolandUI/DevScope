using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RolandUI.DevScope.ValueConverters;

internal class BoolToImageConverter : IValueConverter
{
    public static BoolToImageConverter Shared { get; } = new();

    public IImage? TrueImage { get; set; }

    public IImage? FalseImage { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            true => TrueImage,
            false => FalseImage,
            _ => null,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
