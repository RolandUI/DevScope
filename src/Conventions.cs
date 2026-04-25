namespace ClassicDiagnostics.Avalonia
{
    internal static class Conventions
    {
        public static IScreenshotHandler DefaultScreenshotHandler { get; } =
            new Screenshots.FilePickerHandler();
    }
}
