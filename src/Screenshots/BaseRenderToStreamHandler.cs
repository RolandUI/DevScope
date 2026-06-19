namespace ClassicDiagnostics.Avalonia.Screenshots;

/// <summary>
///     Base class for render Screenshot to stream
/// </summary>
public abstract class BaseRenderToStreamHandler : IScreenshotHandler
{

    public async Task Take(Control control)
    {
#if NET6_0_OR_GREATER
        await using var output = await GetStream(control);
#else
            using var output = await GetStream(control);
#endif
        if (output is not null)
        {
            control.RenderTo(output);
            await output.FlushAsync();
        }
    }

    /// <summary>
    ///     Get stream to write a screenshot to.
    /// </summary>
    /// <param name="control"></param>
    /// <returns>stream to render the control</returns>
    protected abstract Task<Stream?> GetStream(Control control);
}
