using Avalonia.Platform.Storage;
using ClassicDiagnostics.Avalonia.Views;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;

namespace ClassicDiagnostics.Avalonia.Screenshots;

/// <summary>
///     Show a FileSavePicker to select where save screenshot
/// </summary>
public sealed class FilePickerHandler : BaseRenderToStreamHandler
{
    private readonly string? _screenshotRoot;
    private readonly string _title;

    /// <summary>
    ///     Instance FilePickerHandler
    /// </summary>
    public FilePickerHandler() : this(null) { }

    /// <summary>
    ///     Instance FilePickerHandler with specificated parameter
    /// </summary>
    /// <param name="title">SaveFilePicker Title</param>
    /// <param name="screenshotRoot"></param>
    public FilePickerHandler(string? title, string? screenshotRoot = null)
    {
        _title = title ?? "Save Screenshot to ...";
        _screenshotRoot = screenshotRoot;
    }

    private static TopLevel GetTopLevel(Control control)
    {
        // If possible, use devtools main window.
        TopLevel? devToolsTopLevel = null;
        if (Application.Current?.ApplicationLifetime is Lifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            devToolsTopLevel = desktop.Windows.FirstOrDefault(w => w is MainWindow);
        }

        return devToolsTopLevel ?? TopLevel.GetTopLevel(control)
            ?? throw new InvalidOperationException("No TopLevel is available.");
    }

    protected async override Task<Stream?> GetStream(Control control)
    {
        var storageProvider = GetTopLevel(control).StorageProvider;

        IStorageFolder? defaultFolder = null;
        if (_screenshotRoot is not null) defaultFolder = await storageProvider.TryGetFolderFromPathAsync(_screenshotRoot);
        defaultFolder ??= await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Pictures);

        var result = await storageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                SuggestedStartLocation = defaultFolder,
                Title = _title,
                FileTypeChoices = [FilePickerFileTypes.ImagePng],
                DefaultExtension = ".png",
            });
        if (result is null)
        {
            return null;
        }

        return await result.OpenWriteAsync();
    }
}