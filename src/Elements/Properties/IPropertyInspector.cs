namespace RolandUI.DevScope.Elements.Properties;

internal interface IPropertyInspector
{
    PropertyInspectionResult Inspect(object target, PropertyInspectionOptions options);
}