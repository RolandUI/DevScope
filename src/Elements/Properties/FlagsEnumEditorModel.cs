namespace ClassicDiagnostics.Avalonia.Elements.Properties;

internal sealed class FlagsEnumEditorModel
{
    public Type EnumType { get; }

    public IReadOnlyList<FlagsEnumOption> Options { get; }

    public FlagsEnumEditorModel(Type enumType)
    {
        if (!enumType.IsEnum)
        {
            throw new ArgumentException("Flags enum editor model requires an enum type.", nameof(enumType));
        }

        EnumType = enumType;
        Options = CreateOptions(enumType);
    }

    public object Clear()
    {
        return Enum.ToObject(EnumType, 0);
    }

    public bool IsSelected(object? value, FlagsEnumOption option)
    {
        return (ToUInt64(value) & option.RawValue) == option.RawValue;
    }

    public object Toggle(object? value, FlagsEnumOption option, bool isSelected)
    {
        var rawValue = ToUInt64(value);
        var updatedValue = isSelected ?
            rawValue | option.RawValue :
            rawValue & ~option.RawValue;

        return Enum.ToObject(EnumType, updatedValue);
    }

    private static IReadOnlyList<FlagsEnumOption> CreateOptions(Type enumType)
    {
        var options = new List<FlagsEnumOption>();
        var seenValues = new HashSet<ulong>();

        foreach (var name in Enum.GetNames(enumType))
        {
            var value = Enum.Parse(enumType, name);
            var rawValue = ToUInt64(value);
            if (rawValue == 0 || !IsSingleBit(rawValue) || !seenValues.Add(rawValue))
            {
                continue;
            }

            options.Add(new FlagsEnumOption(name, value, rawValue));
        }

        return options;
    }

    private static bool IsSingleBit(ulong value)
    {
        return (value & (value - 1)) == 0;
    }

    private static ulong ToUInt64(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        var valueType = value.GetType();
        var underlyingType = valueType.IsEnum ? Enum.GetUnderlyingType(valueType) : valueType;
        var typeCode = Type.GetTypeCode(underlyingType);

        return typeCode switch
        {
            TypeCode.SByte => unchecked((ulong)Convert.ToSByte(value)),
            TypeCode.Int16 => unchecked((ulong)Convert.ToInt16(value)),
            TypeCode.Int32 => unchecked((ulong)Convert.ToInt32(value)),
            TypeCode.Int64 => unchecked((ulong)Convert.ToInt64(value)),
            _ => Convert.ToUInt64(value),
        };
    }
}