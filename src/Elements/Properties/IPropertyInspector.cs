namespace ClassicDiagnostics.Avalonia.Elements.Properties;

internal interface IPropertyInspector
{
    PropertyInspectionResult Inspect(object target, PropertyInspectionOptions options);
}