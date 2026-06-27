using ClassicDiagnostics.Avalonia.Rooting;
using ClassicDiagnostics.Avalonia.Views.Shell;
using Application = Avalonia.Application;

namespace ClassicDiagnostics.Avalonia.Hosting;

internal static class DevToolsWindowHost
{
    private static MainWindow? DebuggerWindow { get; set; }

    internal static void ShowOrActivate(IDevToolsRootSource rootSource, DevToolsOptions options, Application? application)
    {
        var focusedControl = GetFocusedControl(rootSource);

        if (DebuggerWindow is { } existingWindow)
        {
            Activate(existingWindow, focusedControl);
            return;
        }

        var window = new MainWindow
        {
            Root = CreateRoot(rootSource, application),
            Width = options.Size.Width,
            Height = options.Size.Height,
        };
        window.SetOptions(options);
        window.SelectedControl(focusedControl);
        window.Closed += HandleDebuggerWindowClosed;
        DebuggerWindow = window;

        window.Show();
    }

    private static AvaloniaObject CreateRoot(IDevToolsRootSource rootSource, Application? application)
    {
        return (rootSource, application) switch
        {
            (_, not null) => new ApplicationRootNode(rootSource, application),
            (SingleTopLevelRootSource source, _) => source.TopLevel,
            _ => new PresentationRootNode(rootSource),
        };
    }

    private static Control? GetFocusedControl(IDevToolsRootSource rootSource)
    {
        foreach (var root in rootSource.Items)
        {
            if (root.FocusManager.GetFocusedElement() is not Control control)
            {
                continue;
            }

            // F12 can be pressed while the diagnostics window itself has focus. Selecting
            // DevTools controls would make the inspected tree jump into its own UI.
            if (!control.DoesBelongToDevTool())
            {
                return control;
            }
        }

        return null;
    }

    private static void Activate(MainWindow window, Control? focusedControl)
    {
        window.Activate();
        window.SelectedControl(focusedControl);
    }

    private static void HandleDebuggerWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not MainWindow window)
        {
            return;
        }

        window.Closed -= HandleDebuggerWindowClosed;
        if (ReferenceEquals(DebuggerWindow, window))
        {
            DebuggerWindow = null;
        }
    }
}