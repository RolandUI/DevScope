using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace RolandUI.DevScope.Elements.Properties;

internal static class PropertyStringConversion
{
    private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
    private readonly static Type[] StringParameter = [typeof(string)];
    private readonly static Type[] StringFormatProviderParameters = [typeof(string), typeof(IFormatProvider)];

    public static bool CanConvertFromString(Type type)
    {
        var converter = TypeDescriptor.GetConverter(type);

        if (converter.CanConvertFrom(typeof(string)))
        {
            return true;
        }

        return GetParseMethod(type, out _) is not null;
    }

    public static object? FromString(string value, Type type)
    {
        var converter = TypeDescriptor.GetConverter(type);

        return converter.CanConvertFrom(typeof(string)) ?
            converter.ConvertFrom(null, CultureInfo.InvariantCulture, value) :
            InvokeParse(value, type);
    }

    public static string? ToString(object value)
    {
        var converter = TypeDescriptor.GetConverter(value);

        // CollectionConverter only reports "(Collection)", which hides the useful object display.
        if (!converter.CanConvertTo(typeof(string)) ||
            converter.GetType() == typeof(CollectionConverter))
        {
            return value.ToString();
        }

        return converter.ConvertToInvariantString(value);
    }

    private static object? InvokeParse(string value, Type targetType)
    {
        var method = GetParseMethod(targetType, out var hasFormat);

        if (method is null)
        {
            throw new InvalidOperationException($"Type '{targetType.FullName}' cannot parse string values.");
        }

        return method.Invoke(
            null,
            hasFormat ?
                [value, CultureInfo.InvariantCulture] :
                [value]);
    }

    private static MethodInfo? GetParseMethod(Type type, out bool hasFormat)
    {
        var method = type.GetMethod("Parse", PublicStatic, null, StringFormatProviderParameters, null);

        if (method is not null)
        {
            hasFormat = true;
            return method;
        }

        hasFormat = false;
        return type.GetMethod("Parse", PublicStatic, null, StringParameter, null);
    }
}