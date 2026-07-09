namespace RolandUI.DevScope.Elements.Properties;

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