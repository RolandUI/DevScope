using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

internal interface IPropertyColumnContentViewModel : IDisposable
{
    object Target { get; }

    string Title { get; }

    string Path { get; }

    FilterViewModel Filter { get; }

    void Refresh();
}