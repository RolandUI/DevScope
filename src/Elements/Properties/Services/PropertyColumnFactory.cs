using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.Services;

internal sealed class PropertyColumnFactory(
    IPropertyInspector propertyInspector,
    Func<PropertyInspectionOptions> createOptions,
    Func<bool> showDetailsPropertyType,
    Action<PropertyViewModel> togglePinnedProperty
) : IPropertyColumnFactory
{
    public PropertyColumnViewModel CreateColumn(
        IPropertyColumnOwner owner,
        object target,
        string title,
        string path)
    {
        var kind = PropertyColumnNavigation.GetColumnKind(target);

        return kind switch
        {
            PropertyValueDescriptorKind.Array
                or PropertyValueDescriptorKind.List
                or PropertyValueDescriptorKind.Dictionary
                or PropertyValueDescriptorKind.Enumerable => CreateContainerColumn(owner, target, title, path, kind.Value),
            _ => CreateObjectColumn(owner, target, title, path),
        };
    }

    private PropertyColumnViewModel CreateObjectColumn(
        IPropertyColumnOwner owner,
        object target,
        string title,
        string path)
    {
        var content = new ObjectPropertiesColumnViewModel(
            target,
            title,
            path,
            propertyInspector,
            createOptions,
            showDetailsPropertyType,
            togglePinnedProperty);

        var column = new PropertyColumnViewModel(owner, content);
        content.SelectedPropertyChanged += (_, property) => owner.OpenFrom(column, property);
        return column;
    }

    private static PropertyColumnViewModel CreateContainerColumn(
        IPropertyColumnOwner owner,
        object target,
        string title,
        string path,
        PropertyValueDescriptorKind kind)
    {
        var content = new ContainerPropertiesColumnViewModel(target, title, path, kind);
        var column = new PropertyColumnViewModel(owner, content);
        content.SelectedItemChanged += (_, item) => owner.OpenFrom(column, item);
        return column;
    }
}