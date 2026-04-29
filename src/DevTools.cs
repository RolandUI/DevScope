using System.Collections.Specialized;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Views;
using Application = Avalonia.Application;

namespace ClassicDiagnostics.Avalonia;

internal static class DevTools
{
    private readonly static Dictionary<IDevToolsTopLevelGroup, MainWindow> s_open = new();

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
        return Open(new SingleViewTopLevelGroup(root), options, root as Window, null);
    }

    internal static IDisposable Open(IDevToolsTopLevelGroup group, DevToolsOptions options)
    {
        return Open(group, options, null, null);
    }

    internal static IDisposable Attach(Application application, DevToolsOptions options)
    {
        var openedDisposable = new SerialDisposableValue();
        var result = new CompositeDisposable(2);
        result.Add(openedDisposable);

        // Skip if call on Design Mode
        if (!Design.IsDesignMode)
        {
            if (application.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifeTime)
            {
                throw new ArgumentNullException(
                    nameof(application),
                    "DevTools can only attach to applications that support IClassicDesktopStyleApplicationLifetime.");
            }

            if (application.InputManager is not null)
            {
                result.Add(
                    application.InputManager.PreProcess.Subscribe(e =>
                    {
                        var owner = lifeTime.MainWindow;

                        if (e is RawKeyEventArgs keyEventArgs
                            && keyEventArgs.Type == RawKeyEventType.KeyUp
                            && options.Gesture.Matches(keyEventArgs))
                        {
                            openedDisposable.Disposable =
                                Open(
                                    new ClassicDesktopStyleApplicationLifetimeTopLevelGroup(lifeTime),
                                    options,
                                    owner,
                                    application);
                            e.Handled = true;
                        }
                    }));
            }
        }
        return result;
    }

    private static IDisposable Open(
        IDevToolsTopLevelGroup topLevelGroup,
        DevToolsOptions options,
        Window? owner,
        Application? app)
    {
        var focusedControl = owner?.FocusManager?.GetFocusedElement() as Control;
        AvaloniaObject root = topLevelGroup switch
        {
            ClassicDesktopStyleApplicationLifetimeTopLevelGroup gr => new ApplicationPage(gr, app ?? Application.Current!),
            SingleViewTopLevelGroup gr => gr.Items.First(),
            _ => new TopLevelGroup(topLevelGroup),
        };

        // If single static toplevel is already visible in another devtools window, focus it.
        if (s_open.TryGetValue(topLevelGroup, out var mainWindow))
        {
            mainWindow.Activate();
            mainWindow.SelectedControl(focusedControl);
            return Disposable.Empty;
        }
        if (topLevelGroup.Items.Count == 1 && topLevelGroup.Items is not INotifyCollectionChanged)
        {
            var singleTopLevel = topLevelGroup.Items.First();

            foreach (var group in s_open)
            {
                if (group.Key.Items.Contains(singleTopLevel))
                {
                    group.Value.Activate();
                    group.Value.SelectedControl(focusedControl);
                    return Disposable.Empty;
                }
            }
        }

        var window = new MainWindow
        {
            Root = root,
            Width = options.Size.Width,
            Height = options.Size.Height,
            Tag = topLevelGroup,
        };
        window.SetOptions(options);
        window.SelectedControl(focusedControl);
        window.Closed += DevToolsClosed;
        s_open.Add(topLevelGroup, window);
        if (options.ShowAsChildWindow && owner is not null)
        {
            window.Show(owner);
        }
        else
        {
            window.Show();
        }
        return Disposable.Create(() => window.Close());
    }

    private static void DevToolsClosed(object? sender, EventArgs e)
    {
        var window = (MainWindow)sender!;
        window.Closed -= DevToolsClosed;
        s_open.Remove((IDevToolsTopLevelGroup)window.Tag!);
    }

    internal static bool DoesBelongToDevTool(this Visual v)
    {
        var topLevel = TopLevel.GetTopLevel(v);

        while (topLevel is not null && topLevel is not MainWindow)
        {
            if (topLevel is PopupRoot pr)
            {
                topLevel = pr.ParentTopLevel;
            }
            else
            {
                return false;
            }
        }
        return true;
    }
}

file sealed class SerialDisposableValue : IDisposable
{
    private readonly object _sync = new();
    private IDisposable? _disposable;

    public IDisposable? Disposable
    {
        get => _disposable;
        set
        {
            lock (_sync)
            {
                _disposable?.Dispose();
                _disposable = value;
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _disposable?.Dispose();
            _disposable = null;
        }
    }
}