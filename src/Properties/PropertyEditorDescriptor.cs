using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

namespace ClassicDiagnostics.Avalonia.Properties;

internal readonly record struct PropertyEditorDescriptor(
    PropertyEditorKind Kind,
    Type PropertyType,
    bool IsReadOnly,
    bool CanEdit,
    bool CanNavigate);
