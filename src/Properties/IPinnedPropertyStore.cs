using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal interface IPinnedPropertyStore
{
    bool Contains(string key);

    bool Pin(string key);

    bool Toggle(string key);

    bool Unpin(string key);
}
