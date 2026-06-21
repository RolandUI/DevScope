using System.Collections;

namespace ClassicDiagnostics.Avalonia.Properties;

internal sealed class PropertyValueDescriptorFactory
{
    private const int MaxEnumerableChildren = 256;

    public static PropertyValueDescriptorFactory Default { get; } = new();

    private PropertyValueDescriptorFactory()
    {
    }

    public PropertyValueDescriptor Create(object? value)
    {
        if (value is null)
        {
            return CreateSimple(PropertyValueDescriptorKind.Null, typeof(object), canNavigate: false);
        }

        var valueType = value.GetType();

        if (value is string || valueType.IsValueType || PropertyStringConversion.CanConvertFromString(valueType))
        {
            return CreateSimple(PropertyValueDescriptorKind.Simple, valueType, canNavigate: false);
        }

        if (value is Array array)
        {
            return new PropertyValueDescriptor(
                PropertyValueDescriptorKind.Array,
                valueType,
                array.Length,
                isReadOnly: true,
                canNavigate: true,
                CreateIndexedChildren(array.Cast<object?>()));
        }

        if (value is IDictionary dictionary)
        {
            return new PropertyValueDescriptor(
                PropertyValueDescriptorKind.Dictionary,
                valueType,
                dictionary.Count,
                isReadOnly: true,
                canNavigate: true,
                CreateDictionaryChildren(dictionary));
        }

        if (value is IList list)
        {
            return new PropertyValueDescriptor(
                PropertyValueDescriptorKind.List,
                valueType,
                list.Count,
                isReadOnly: true,
                canNavigate: true,
                CreateIndexedChildren(list.Cast<object?>()));
        }

        if (value is IEnumerable enumerable)
        {
            return new PropertyValueDescriptor(
                PropertyValueDescriptorKind.Enumerable,
                valueType,
                count: null,
                isReadOnly: true,
                canNavigate: true,
                CreateIndexedChildren(enumerable.Cast<object?>().Take(MaxEnumerableChildren)));
        }

        return CreateSimple(PropertyValueDescriptorKind.Object, valueType, canNavigate: true);
    }

    private static PropertyValueDescriptor CreateSimple(
        PropertyValueDescriptorKind kind,
        Type valueType,
        bool canNavigate)
    {
        return new PropertyValueDescriptor(
            kind,
            valueType,
            count: null,
            isReadOnly: true,
            canNavigate,
            []);
    }

    private static IReadOnlyList<PropertyValueChildDescriptor> CreateIndexedChildren(IEnumerable<object?> values)
    {
        return values
            .Select((value, index) => new PropertyValueChildDescriptor($"[{index}]", value))
            .ToArray();
    }

    private static IReadOnlyList<PropertyValueChildDescriptor> CreateDictionaryChildren(IDictionary dictionary)
    {
        return dictionary
            .Cast<object>()
            .Select(CreateDictionaryChild)
            .ToArray();
    }

    private static PropertyValueChildDescriptor CreateDictionaryChild(object entry)
    {
        if (entry is DictionaryEntry dictionaryEntry)
        {
            return new PropertyValueChildDescriptor($"[{dictionaryEntry.Key}]", dictionaryEntry.Value);
        }

        var entryType = entry.GetType();
        var key = entryType.GetProperty("Key")?.GetValue(entry);
        var value = entryType.GetProperty("Value")?.GetValue(entry);

        return new PropertyValueChildDescriptor($"[{key}]", value);
    }
}
