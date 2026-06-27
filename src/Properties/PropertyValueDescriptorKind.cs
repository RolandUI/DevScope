using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal enum PropertyValueDescriptorKind
{
    Object,
    Array,
    List,
    Dictionary,
    Enumerable,
    Null,
    Simple,
}
