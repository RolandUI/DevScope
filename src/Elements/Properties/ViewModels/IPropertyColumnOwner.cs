namespace ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

internal interface IPropertyColumnOwner
{
    void OpenFrom(PropertyColumnViewModel sourceColumn, PropertyViewModel? property);

    void OpenFrom(PropertyColumnViewModel sourceColumn, IPropertyColumnItemViewModel? item);

    void CloseFrom(PropertyColumnViewModel column);

    void RememberWidth(PropertyColumnViewModel column);
}