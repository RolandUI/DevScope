using ClassicDiagnostics.Avalonia.Hosting;
using Application = Avalonia.Application;

namespace ClassicDiagnostics.Avalonia;

public static class DevTools
{
    /// <summary>
    ///     Attaches DevTools to an Application, to be opened with the specified options.
    /// </summary>
    /// <param name="application"></param>
    /// <param name="options">Additional settings of DevTools.</param>
    /// <remarks>
    ///     Attach DevTools should only be called after application initialization is complete. A good point is
    ///     <see cref="Application.OnFrameworkInitializationCompleted" />
    /// </remarks>
    /// <example>
    ///     <code>
    /// public class App : Application
    /// {
    ///    public override void OnFrameworkInitializationCompleted()
    ///    {
    ///       if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
    ///       {
    ///          desktopLifetime.MainWindow = new ShellWindow();
    ///       }
    ///       else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewLifetime)
    ///          singleViewLifetime.MainView = new MainView();
    ///
    ///       base.OnFrameworkInitializationCompleted();
    ///       this.AttachDevTools(new ClassicDiagnostics.Avalonia.DevToolsOptions()
    ///           {
    ///              StartupScreenIndex = 1,
    ///           });
    ///    }
    /// }
    /// </code>
    /// </example>
    public static IDisposable AttachDevTools(this Application application, DevToolsOptions? options = null)
    {
        return Design.IsDesignMode ? Disposable.Empty : new DevToolsApplicationSession(application, options ?? new DevToolsOptions());
    }
}