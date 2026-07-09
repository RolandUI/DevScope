using System.Collections;

namespace RolandUI.DevScope.Elements.Properties;

internal static class PropertyValueDescriptorFactory
{
    public static PropertyValueDescriptor Create(object? value)
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
                isReadOnly: false,
                canNavigate: true,
                Enumerable.Range(0, array.Length)
                    .Select(index => CreateArrayChild(array, index))
                    .ToArray());
        }

        if (value is IDictionary dictionary)
        {
            return new PropertyValueDescriptor(
                PropertyValueDescriptorKind.Dictionary,
                valueType,
                dictionary.Count,
                dictionary.IsReadOnly,
                canNavigate: true,
                CreateDictionaryChildren(dictionary));
        }

        if (value is IList list)
        {
            return new PropertyValueDescriptor(
                PropertyValueDescriptorKind.List,
                valueType,
                list.Count,
                list.IsReadOnly,
                canNavigate: true,
                Enumerable.Range(0, list.Count)
                    .Select(index => CreateListChild(list, index))
                    .ToArray());
        }

        if (value is IEnumerable)
        {
            return new PropertyValueDescriptor(
                PropertyValueDescriptorKind.Enumerable,
                valueType,
                count: null,
                isReadOnly: true,
                canNavigate: true,
                []);
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

    internal static PropertyValueChildDescriptor CreateArrayChild(Array array, int linearIndex)
    {
        var indices = GetArrayIndices(array, linearIndex);
        var name = $"[{string.Join(",", indices)}]";
        var valueType = array.GetType().GetElementType() ?? typeof(object);

        return new PropertyValueChildDescriptor(
            name,
            valueType,
            () => array.GetValue(indices),
            value => WriteChild(name, valueType, value, () => array.SetValue(value, indices), () => array.GetValue(indices)));
    }

    internal static PropertyValueChildDescriptor CreateListChild(IList list, int index)
    {
        var name = $"[{index}]";
        var valueType = GetGenericInterfaceArgument(list.GetType(), typeof(IList<>), argumentIndex: 0)
            ?? list[index]?.GetType()
            ?? typeof(object);

        return new PropertyValueChildDescriptor(
            name,
            valueType,
            () => list[index],
            list.IsReadOnly
                ? null
                : value => WriteChild(name, valueType, value, () => list[index] = value, () => list[index]));
    }

    internal static PropertyValueChildDescriptor CreateDictionaryChild(IDictionary dictionary, object? key)
    {
        var name = $"[{key}]";
        var valueType = GetGenericInterfaceArgument(dictionary.GetType(), typeof(IDictionary<,>), argumentIndex: 1)
            ?? dictionary[key!]?.GetType()
            ?? typeof(object);

        return new PropertyValueChildDescriptor(
            name,
            valueType,
            () => dictionary[key!],
            dictionary.IsReadOnly
                ? null
                : value => WriteChild(name, valueType, value, () => dictionary[key!] = value, () => dictionary[key!]));
    }

    internal static PropertyValueChildDescriptor CreateReadOnlyChild(string name, object? value)
    {
        return new PropertyValueChildDescriptor(name, value?.GetType() ?? typeof(object), () => value);
    }

    private static IReadOnlyList<PropertyValueChildDescriptor> CreateDictionaryChildren(IDictionary dictionary)
    {
        return dictionary.Keys
            .Cast<object?>()
            .Select(key => CreateDictionaryChild(dictionary, key))
            .ToArray();
    }

    private static int[] GetArrayIndices(Array array, int linearIndex)
    {
        var indices = new int[array.Rank];

        for (var dimension = array.Rank - 1; dimension >= 0; dimension--)
        {
            var length = array.GetLength(dimension);
            indices[dimension] = array.GetLowerBound(dimension) + linearIndex % length;
            linearIndex /= length;
        }

        return indices;
    }

    private static Type? GetGenericInterfaceArgument(Type type, Type interfaceType, int argumentIndex)
    {
        return type
            .GetInterfaces()
            .Prepend(type)
            .FirstOrDefault(candidate => candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == interfaceType)
            ?.GetGenericArguments()[argumentIndex];
    }

    private static PropertyWriteResult WriteChild(
        string name,
        Type valueType,
        object? value,
        Action write,
        Func<object?> read)
    {
        try
        {
            EnsureAssignable(value, valueType);
            write();
            return PropertyWriteResult.Success(read());
        }
        catch (Exception exception)
        {
            var baseException = exception.GetBaseException();
            var message = $"Failed to set collection item '{name}': {baseException.Message}";
            DevToolsDiagnostics.Report(exception, message);
            return PropertyWriteResult.Failure(baseException, message);
        }
    }

    private static void EnsureAssignable(object? value, Type valueType)
    {
        var targetType = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (value is null)
        {
            if (valueType.IsValueType && Nullable.GetUnderlyingType(valueType) is null)
            {
                throw new InvalidCastException($"Null cannot be assigned to '{valueType.GetDetailedTypeName()}'.");
            }

            return;
        }

        if (!targetType.IsInstanceOfType(value))
        {
            throw new InvalidCastException(
                $"Value of type '{value.GetType().GetDetailedTypeName()}' cannot be assigned to '{valueType.GetDetailedTypeName()}'.");
        }
    }
}
