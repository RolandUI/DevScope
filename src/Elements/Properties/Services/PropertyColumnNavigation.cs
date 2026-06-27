using System.Collections;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.Services;

internal static class PropertyColumnNavigation
{
    public static bool CanNavigate(object? value)
    {
        return GetColumnKind(value).HasValue;
    }

    public static PropertyValueDescriptorKind? GetColumnKind(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var valueType = value.GetType();

        if (value is string || valueType.IsValueType || PropertyStringConversion.CanConvertFromString(valueType))
        {
            return null;
        }

        return value switch
        {
            Array => PropertyValueDescriptorKind.Array,
            IDictionary => PropertyValueDescriptorKind.Dictionary,
            IList => PropertyValueDescriptorKind.List,
            IEnumerable => PropertyValueDescriptorKind.Enumerable,
            _ => PropertyValueDescriptorKind.Object,
        };
    }

    public static string GetDisplayName(string name)
    {
        var lastSeparatorIndex = name.LastIndexOf('.');
        return lastSeparatorIndex >= 0 ? name[(lastSeparatorIndex + 1)..] : name;
    }
}