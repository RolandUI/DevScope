namespace ClassicDiagnostics.Avalonia.Properties;

internal readonly record struct PropertyInspectionOptions(
    bool ShowImplementedInterfaces,
    IPinnedPropertyStore PinnedProperties)
{
    public static PropertyInspectionOptions Default => new(
        ShowImplementedInterfaces: false,
        PinnedProperties: new PinnedPropertyStore());
}
