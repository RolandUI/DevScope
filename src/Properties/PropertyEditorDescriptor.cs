namespace ClassicDiagnostics.Avalonia.Properties;

internal readonly record struct PropertyEditorDescriptor(
    PropertyEditorKind Kind,
    Type PropertyType,
    bool IsReadOnly,
    bool CanEdit,
    bool CanNavigate);
