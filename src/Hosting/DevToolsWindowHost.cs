using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Views;
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
        window.Closed += OnDebuggerWindowClosed;
        DebuggerWindow = window;

        window.Show();
    }

    private static AvaloniaObject CreateRoot(IDevToolsRootSource rootSource, Application? application)
    {
        return (rootSource, application) switch
        {
            (_, not null) => new ApplicationPage(rootSource, application),
            (SingleTopLevelRootSource source, _) => source.TopLevel,
            _ => new PresentationRootGroup(rootSource),
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

    private static void OnDebuggerWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not MainWindow window)
        {
            return;
        }

        window.Closed -= OnDebuggerWindowClosed;
        if (ReferenceEquals(DebuggerWindow, window))
        {
            DebuggerWindow = null;
        }
    }
}
