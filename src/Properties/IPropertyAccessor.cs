namespace ClassicDiagnostics.Avalonia.Properties;

internal interface IPropertyAccessor
{
    Type AssignedType { get; }

    Type? DeclaringType { get; }

    string Group { get; }

    bool? IsAttached { get; }

    bool IsReadOnly { get; }

    object Key { get; }

    string Name { get; }

    string Priority { get; }

    Type PropertyType { get; }

    object? Value { get; }

    void Update();

    PropertyWriteResult Write(object? value);
}
