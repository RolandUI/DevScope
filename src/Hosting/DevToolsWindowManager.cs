using System.Collections.Specialized;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Views;
using Application = Avalonia.Application;

namespace ClassicDiagnostics.Avalonia.Hosting;

internal sealed class DevToolsWindowManager
{
    private readonly Dictionary<IDevToolsTopLevelGroup, MainWindow> _openWindows = new();

    public IDisposable Open(TopLevel root, DevToolsOptions options)
    {
        return Open(new SingleViewTopLevelGroup(root), options, root as Window, null);
    }

    public IDisposable Open(IDevToolsTopLevelGroup group, DevToolsOptions options)
    {
        return Open(group, options, null, null);
    }

    public IDisposable Open(
        IDevToolsTopLevelGroup topLevelGroup,
        DevToolsOptions options,
        Window? owner,
        Application? app)
    {
        var focusedControl = owner?.FocusManager.GetFocusedElement() as Control;
        var root = CreateRoot(topLevelGroup, app);

        if (TryActivateExisting(topLevelGroup, focusedControl))
        {
            return Disposable.Empty;
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
        window.Closed += OnDevToolsClosed;
        _openWindows.Add(topLevelGroup, window);

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

    private static AvaloniaObject CreateRoot(IDevToolsTopLevelGroup topLevelGroup, Application? app)
    {
        return topLevelGroup switch
        {
            ClassicDesktopStyleApplicationLifetimeTopLevelGroup group => new ApplicationPage(
                group,
                app ?? Application.Current!),
            SingleViewTopLevelGroup group => group.Items.First(),
            _ => new TopLevelGroup(topLevelGroup),
        };
    }

    private bool TryActivateExisting(IDevToolsTopLevelGroup topLevelGroup, Control? focusedControl)
    {
        if (_openWindows.TryGetValue(topLevelGroup, out var mainWindow))
        {
            Activate(mainWindow, focusedControl);
            return true;
        }

        if (topLevelGroup.Items.Count != 1 || topLevelGroup.Items is INotifyCollectionChanged)
        {
            return false;
        }

        var singleTopLevel = topLevelGroup.Items[0];

        // Single TopLevel attachments can be represented by different group instances.
        // Keep the legacy behavior by reusing any DevTools window that already owns it.
        foreach (var openWindow in _openWindows.Where(x => x.Key.Items.Contains(singleTopLevel)))
        {
            Activate(openWindow.Value, focusedControl);
            return true;
        }

        return false;
    }

    private static void Activate(MainWindow window, Control? focusedControl)
    {
        window.Activate();
        window.SelectedControl(focusedControl);
    }

    private void OnDevToolsClosed(object? sender, EventArgs e)
    {
        var window = (MainWindow)sender!;
        window.Closed -= OnDevToolsClosed;
        _openWindows.Remove((IDevToolsTopLevelGroup)window.Tag!);
    }
}
