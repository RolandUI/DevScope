using RolandUI.DevScope.Elements.Properties.ViewModels;

namespace RolandUI.DevScope.Elements.Properties.Services;

internal interface IPropertyColumnFactory
{
    PropertyColumnViewModel CreateColumn(
        IPropertyColumnOwner owner,
        object target,
        string title,
        string path);
}