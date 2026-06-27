using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal enum PropertyEditorKind
{
    Boolean,
    Numeric,
    Color,
    Brush,
    Image,
    Geometry,
    Enum,
    FlagsEnum,
    Text,
    ReadOnlyText,
    ComplexObject,
}
