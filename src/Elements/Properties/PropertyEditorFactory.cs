using Avalonia.Media;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties;

internal sealed class PropertyEditorFactory : IPropertyEditorFactory
{
    public static PropertyEditorFactory Default { get; } = new();

    private PropertyEditorFactory()
    {
    }

    public PropertyEditorDescriptor Create(PropertyViewModel property)
    {
        var propertyType = property.PropertyType;
        var valueDescriptor = PropertyValueDescriptorFactory.Create(property.Value);
        var kind = GetKind(propertyType, property.IsReadonly, valueDescriptor);

        return new PropertyEditorDescriptor(
            kind,
            propertyType,
            property.IsReadonly,
            CanEdit: CanEdit(kind, property.IsReadonly),
            CanNavigate: valueDescriptor.CanNavigate);
    }

    private static bool CanEdit(PropertyEditorKind kind, bool isReadOnly)
    {
        return !isReadOnly && kind is not PropertyEditorKind.ComplexObject and not PropertyEditorKind.ReadOnlyText;
    }

    private static PropertyEditorKind GetKind(
        Type propertyType,
        bool isReadOnly,
        PropertyValueDescriptor valueDescriptor)
    {
        if (propertyType == typeof(bool))
        {
            return PropertyEditorKind.Boolean;
        }

        if (IsValidNumeric(propertyType))
        {
            return PropertyEditorKind.Numeric;
        }

        if (propertyType == typeof(Color))
        {
            return PropertyEditorKind.Color;
        }

        if (ImplementsInterface<IBrush>(propertyType))
        {
            return PropertyEditorKind.Brush;
        }

        if (ImplementsInterface<IImage>(propertyType))
        {
            return PropertyEditorKind.Image;
        }

        if (propertyType == typeof(Geometry))
        {
            return PropertyEditorKind.Geometry;
        }

        if (propertyType.IsEnum)
        {
            return propertyType.IsDefined(typeof(FlagsAttribute), false) ?
                PropertyEditorKind.FlagsEnum :
                PropertyEditorKind.Enum;
        }

        if (propertyType != typeof(object) && PropertyStringConversion.CanConvertFromString(propertyType))
        {
            return isReadOnly ? PropertyEditorKind.ReadOnlyText : PropertyEditorKind.Text;
        }

        return valueDescriptor.CanNavigate ? PropertyEditorKind.ComplexObject : PropertyEditorKind.ReadOnlyText;
    }

    private static bool ImplementsInterface<TInterface>(Type type)
    {
        var interfaceType = typeof(TInterface);
        return type == interfaceType || interfaceType.IsAssignableFrom(type);
    }

    private static bool IsValidNumeric(Type? type)
    {
        if (type == null || type.IsEnum)
        {
            return false;
        }

        var typeCode = Type.GetTypeCode(type);
        if (typeCode == TypeCode.Object)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                typeCode = Type.GetTypeCode(Nullable.GetUnderlyingType(type));
            }
            else
            {
                return false;
            }
        }

        switch (typeCode)
        {
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.SByte:
            case TypeCode.Single:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }
}