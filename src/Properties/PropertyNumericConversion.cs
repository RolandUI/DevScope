using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal static class PropertyNumericConversion
{
    public static decimal ToDecimal(object? value)
    {
        return Convert.ToDecimal(value);
    }

    public static object? FromDecimal(object? value, Type targetType)
    {
        if (value is null)
        {
            return null;
        }

        var conversionType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return Convert.ChangeType(value, conversionType);
    }
}
