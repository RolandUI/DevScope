namespace ClassicDiagnostics.Avalonia;

/// <summary>
///     Allowed to define custom handler for Shreeshot
/// </summary>
public interface IScreenshotHandler
{
    /// <summary>
    ///     Handle the Screenshot
    /// </summary>
    /// <returns></returns>
    Task Take(Control control);
}