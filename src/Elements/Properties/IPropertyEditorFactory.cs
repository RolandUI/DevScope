using RolandUI.DevScope.Elements.Properties.ViewModels;

namespace RolandUI.DevScope.Elements.Properties;

internal interface IPropertyEditorFactory
{
    PropertyEditorDescriptor Create(PropertyViewModel property);
}