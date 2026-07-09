namespace RolandUI.DevScope;

/// <summary>
///     Allowed to define custom handler for Screenshot, which can be used to save the screenshot to a file, or to copy it to the clipboard, etc.
/// </summary>
public interface IScreenshotHandler
{
    /// <summary>
    ///     Handle the Screenshot
    /// </summary>
    /// <returns></returns>
    Task Take(Control control);
}