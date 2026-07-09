using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RolandUI.DevScope.ValueConverters;

internal static class CommonConverters
{
    public static IValueConverter ObjectToString { get; } = new FuncValueConverter<object?, string?>(convert: x => x?.ToString());

    public static IValueConverter TypeEquals { get; } = new FuncValueConverter<object?, object?, bool>(
        convert: (x, parameter) => x?.GetType() == parameter as Type
    );

    public new static IValueConverter GetType { get; } = new FuncValueConverter<object?, object?>(
        convert: x => x?.GetType()
    );

    public static IValueConverter StringToUri { get; } = new FuncValueConverter<string?, Uri?>(
        convert: x => Uri.TryCreate(x, UriKind.RelativeOrAbsolute, out var uri) ? uri : null,
        convertBack: x => x?.ToString()
    );

    public static IValueConverter ColorToBrush { get; } = new FuncValueConverter<Color, SolidColorBrush>(
        convert: color => new SolidColorBrush(color)
    );

    public static IValueConverter DateTimeOffsetToString { get; } = new FuncValueConverter<DateTimeOffset, object?, string>(
        convert: (x, p) => x.DateTime.ToLocalTime().ToString(p?.ToString()),
        convertBack: (x, p) => DateTimeOffset.ParseExact(x ?? string.Empty, p?.ToString() ?? "o", null)
    );

    public static IValueConverter TimeSpanToSeconds { get; } = new FuncValueConverter<TimeSpan, string?, string>(
        convert: (x, format) => x.TotalSeconds.ToString(format));

    public static IValueConverter FullPathToFileName { get; } = new FuncValueConverter<string, string?>(
        convert: x => Path.GetFileName(x) is { Length: > 0 } fileName ? fileName : x // return original if no file name found (e.g. Path root)
    );

    /// <summary>
    /// Converts an Enum Type to its values array.
    /// </summary>
    public static IValueConverter EnumTypeToValues { get; } = new FuncValueConverter<Type?, Type?, Array?>(
        convert: (x, parameter) =>
        {
            var type = x ?? parameter;
            return type?.IsEnum is true ? Enum.GetValues(type) : null;
        });

    public static IValueConverter IndexFromContainer { get; } = new FuncValueConverter<object?, int>(
        convert: x =>
        {
            if (x is not Control itemContainer) return -1;
            var itemsControl = ItemsControl.ItemsControlFromItemContainer(itemContainer);
            return itemsControl?.IndexFromContainer(itemContainer) ?? -1;
        });

    public static IMultiValueConverter AllEquals { get; } = new AllEqualsConverter();

    /// <summary>
    /// Returns the first non-null and non-UnsetValue value from the input values.
    /// </summary>
    public static IMultiValueConverter FirstNotNull { get; } = new FirstNonNullConverter();

    private class AllEqualsConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Any(v => v == AvaloniaProperty.UnsetValue)) return AvaloniaProperty.UnsetValue;
            var firstValue = values[0];
            return values.All(v => Equals(v, firstValue));
        }
    }

    private class FirstNonNullConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            return values.OfType<object>().FirstOrDefault(value => value != AvaloniaProperty.UnsetValue);
        }
    }
}