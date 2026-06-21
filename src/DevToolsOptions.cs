using Avalonia.Media;

namespace ClassicDiagnostics.Avalonia;

/// <summary>
///     Describes options used to customize DevTools.
/// </summary>
public class DevToolsOptions
{
    /// <summary>
    ///     Gets or sets the key gesture used to open DevTools.
    /// </summary>
    public KeyGesture Gesture { get; set; } = new(Key.F12);

    /// <summary>
    ///     Gets or sets the legacy owner-window display preference.
    /// </summary>
    /// <remarks>
    ///     DevTools now uses one global ownerless diagnostics window, so this option is kept only
    ///     for source compatibility with older setup code.
    /// </remarks>
    [Obsolete("DevTools now uses one global ownerless diagnostics window; this option is ignored.")]
    public bool ShowAsChildWindow { get; set; } = true;

    /// <summary>
    ///     Gets or sets the initial size of the DevTools window. The default value is 1280x720.
    /// </summary>
    public Size Size { get; set; } = new(1280, 720);

    /// <summary>
    ///     Get or set the startup screen index where the DevTools window will be displayed.
    /// </summary>
    public int? StartupScreenIndex { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether DevTools should be displayed implemented interfaces on Control details. The
    ///     default value is true.
    /// </summary>
    public bool ShowImplementedInterfaces { get; set; } = true;

    /// <summary>
    ///     Allow to customize ScreenshotHandler
    /// </summary>
    /// <remarks>Default handler is <see cref="Screenshots.FilePickerHandler" /></remarks>
    public IScreenshotHandler ScreenshotHandler { get; set; }
        = Conventions.DefaultScreenshotHandler;

    /// <summary>
    ///     Gets or sets whether DevTools theme.
    /// </summary>
    public ThemeVariant? ThemeVariant { get; set; }

    /// <summary>
    ///     Get or set Focus Highlighter <see cref="Brush" />
    /// </summary>
    public IBrush? FocusHighlighterBrush { get; set; }

    /// <summary>
    ///     Set the <see cref="DevToolsViewKind">kind</see> of diagnostic view that show at launch of DevTools
    /// </summary>
    public DevToolsViewKind LaunchView { get; init; }

    /// <summary>
    ///     Gets or inits the <see cref="HotKeyConfiguration" /> used to activate DevTools features
    /// </summary>
    public HotKeyConfiguration HotKeys { get; init; } = new();
}