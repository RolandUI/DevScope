using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Elements.Properties.ViewModels;

internal interface IPropertyColumnContentViewModel : IDisposable
{
    object Target { get; }

    string Title { get; }

    string Path { get; }

    FilterViewModel Filter { get; }

    void Refresh();
}