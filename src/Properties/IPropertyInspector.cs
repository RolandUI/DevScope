namespace ClassicDiagnostics.Avalonia.Properties;

internal interface IPropertyInspector
{
    PropertyInspectionResult Inspect(object target, PropertyInspectionOptions options);
}
