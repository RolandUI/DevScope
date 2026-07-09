namespace RolandUI.DevScope.Elements.Properties.ViewModels;

internal interface IPropertyColumnItemViewModel
{
    string Name { get; }

    object? Value { get; }

    bool CanNavigate { get; }
}