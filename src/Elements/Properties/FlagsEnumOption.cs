namespace ClassicDiagnostics.Avalonia.Elements.Properties;

internal readonly record struct FlagsEnumOption(
    string Name,
    object Value,
    ulong RawValue
);