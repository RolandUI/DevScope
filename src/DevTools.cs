using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ClassicDiagnostics.Avalonia.Hosting;
using ClassicDiagnostics.Avalonia.Views;
using Application = Avalonia.Application;

namespace ClassicDiagnostics.Avalonia;

internal static class DevTools
{
    private readonly static DevToolsWindowManager WindowManager = new();

    public static IDisposable Attach(TopLevel root, KeyGesture gesture)
    {
        return Attach(
            root,
            new DevToolsOptions
            {
                Gesture = gesture,
            });
    }

    public static IDisposable Attach(TopLevel root, DevToolsOptions options)
    {
        void PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (options.Gesture.Matches(e))
            {
                Open(root, options);
            }
        }

        return (root ?? throw new ArgumentNullException(nameof(root))).AddDisposableHandler(
            InputElement.KeyDownEvent,
            PreviewKeyDown,
            RoutingStrategies.Tunnel);
    }

    public static IDisposable Open(TopLevel root, DevToolsOptions options)
    {
        return WindowManager.Open(root, options);
    }

    internal static IDisposable Open(IDevToolsTopLevelGroup group, DevToolsOptions options)
    {
        return WindowManager.Open(group, options);
    }

    internal static IDisposable Attach(Application application, DevToolsOptions options)
    {
        // Keep design-mode attachment inert just like the legacy inline implementation.
        return Design.IsDesignMode ?
            Disposable.Empty :
            new DevToolsApplicationSession(application, options, WindowManager);
    }

    internal static bool DoesBelongToDevTool(this Visual v)
    {
        var topLevel = TopLevel.GetTopLevel(v);

        while (topLevel is not null && topLevel is not MainWindow)
        {
            if (topLevel is PopupRoot popupRoot)
            {
                topLevel = popupRoot.ParentTopLevel;
            }
            else
            {
                return false;
            }
        }
        return true;
    }
}
