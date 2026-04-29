using ClassicDiagnostics.Avalonia.Screenshots;

namespace ClassicDiagnostics.Avalonia;

internal static class Conventions
{
    public static IScreenshotHandler DefaultScreenshotHandler { get; } =
        new FilePickerHandler();
}