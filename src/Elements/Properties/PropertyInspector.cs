using System.Reflection;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties;

internal sealed class PropertyInspector : IPropertyInspector
{
    public static PropertyInspector Default { get; } = new();

    private PropertyInspector()
    {
    }

    public PropertyInspectionResult Inspect(object target, PropertyInspectionOptions options)
    {
        var properties = GetProperties(target, options.ShowImplementedInterfaces)
            .Do(property =>
            {
                property.IsPinned = options.PinnedProperties.Contains(property.FullName);
            })
            .ToArray();

        var propertyIndex = properties
            .GroupBy(property => property.Key)
            .ToDictionary(group => group.Key, group => group.ToArray());

        return new PropertyInspectionResult(properties, propertyIndex);
    }

    private static IEnumerable<PropertyViewModel> GetProperties(object target, bool showImplementedInterfaces)
    {
        var descriptor = PropertyValueDescriptorFactory.Create(target);

        if (descriptor.Kind is PropertyValueDescriptorKind.Array
            or PropertyValueDescriptorKind.List
            or PropertyValueDescriptorKind.Enumerable
            or PropertyValueDescriptorKind.Dictionary)
        {
            return GetChildProperties(target, descriptor);
        }

        return GetAvaloniaProperties(target)
            .Concat(GetClrProperties(target, showImplementedInterfaces));
    }

    private static IEnumerable<PropertyViewModel> GetChildProperties(
        object target,
        PropertyValueDescriptor descriptor)
    {
        var group = descriptor.Kind == PropertyValueDescriptorKind.Dictionary ? "Entries" : "Items";

        return descriptor.Children
            .Select(child => new PropertyValueChildViewModel(child, target.GetType(), group));
    }

    private static IEnumerable<PropertyViewModel> GetAvaloniaProperties(object target)
    {
        if (target is not AvaloniaObject avaloniaObject)
        {
            return [];
        }

        return AvaloniaPropertyRegistry.Instance.GetRegistered(avaloniaObject)
            .Union(AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(avaloniaObject.GetType()))
            .Select(property => new AvaloniaPropertyViewModel(avaloniaObject, property));
    }

    private static IEnumerable<PropertyViewModel> GetClrProperties(object target, bool showImplementedInterfaces)
    {
        foreach (var property in GetClrProperties(target, target.GetType()))
        {
            yield return property;
        }

        if (!showImplementedInterfaces)
        {
            yield break;
        }

        foreach (var type in target.GetType().GetInterfaces())
        {
            foreach (var property in GetClrProperties(target, type))
            {
                yield return property;
            }
        }
    }

    private static IEnumerable<PropertyViewModel> GetClrProperties(object target, Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(IsInspectableProperty)
            .Select(property => new ClrPropertyViewModel(target, property));
    }

    private static bool IsInspectableProperty(PropertyInfo property)
    {
        return property.GetIndexParameters().Length == 0;
    }
}