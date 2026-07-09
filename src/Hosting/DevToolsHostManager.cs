using Avalonia.Controls.Primitives;
using RolandUI.DevScope.Rooting;
using RolandUI.DevScope.Shell;
using RolandUI.DevScope.Views.Shell;
using Application = Avalonia.Application;

namespace RolandUI.DevScope.Hosting;

internal interface IDevToolsSurfaceHost : IDisposable
{
    event EventHandler? Closed;

    DevToolsHostKind Kind { get; }

    void ShowOrActivate(Control? focusedControl);

    void Close();
}

internal interface IDevToolsSurfaceHostFactory
{
    IDevToolsSurfaceHost Create(
        IDevToolsRootSource rootSource,
        DevToolsOptions options,
        Application application);
}

internal sealed class DevToolsHostManager : IDisposable
{
    private readonly Application _application;
    private readonly DevToolsOptions _options;
    private readonly IDevToolsRootSource _rootSource;
    private readonly IDevToolsSurfaceHostFactory _surfaceFactory;
    private IDevToolsSurfaceHost? _activeHost;
    private bool _isDisposed;

    public DevToolsHostManager(
        IDevToolsRootSource rootSource,
        DevToolsOptions options,
        Application application,
        IDevToolsSurfaceHostFactory? surfaceFactory = null)
    {
        _rootSource = rootSource;
        _options = options;
        _application = application;
        _surfaceFactory = surfaceFactory ?? DevToolsSurfaceHostFactory.Instance;
    }

    internal bool HasActiveHost => _activeHost is not null;

    internal DevToolsHostKind? ActiveKind => _activeHost?.Kind;

    public void ShowOrActivate()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var focusedControl = GetFocusedControl(_rootSource);
        if (_activeHost is null)
        {
            _activeHost = _surfaceFactory.Create(_rootSource, _options, _application);
            _activeHost.Closed += HandleHostClosed;
        }

        _activeHost.ShowOrActivate(focusedControl);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        var host = _activeHost;
        _activeHost = null;
        if (host is not null)
        {
            host.Closed -= HandleHostClosed;
            host.Close();
            host.Dispose();
        }

        _isDisposed = true;
    }

    private void HandleHostClosed(object? sender, EventArgs e)
    {
        if (sender is not IDevToolsSurfaceHost host || !ReferenceEquals(host, _activeHost))
        {
            return;
        }

        host.Closed -= HandleHostClosed;
        _activeHost = null;
        host.Dispose();
    }

    private static Control? GetFocusedControl(IDevToolsRootSource rootSource)
    {
        foreach (var root in rootSource.Items)
        {
            if (root.FocusManager.GetFocusedElement() is not Control control)
            {
                continue;
            }

            if (!control.DoesBelongToDevTool())
            {
                return control;
            }
        }

        return null;
    }
}

internal sealed class DevToolsSurfaceHostFactory : IDevToolsSurfaceHostFactory
{
    public static DevToolsSurfaceHostFactory Instance { get; } = new();

    private DevToolsSurfaceHostFactory()
    {
    }

    public IDevToolsSurfaceHost Create(
        IDevToolsRootSource rootSource,
        DevToolsOptions options,
        Application application)
    {
        return rootSource.HostKind switch
        {
            DevToolsHostKind.EmbeddedSingleView when rootSource is SingleViewApplicationRootSource singleView =>
                new EmbeddedDevToolsSurfaceHost(singleView, options, application),
            _ => new DesktopDevToolsSurfaceHost(rootSource, options, application),
        };
    }
}

internal sealed class DesktopDevToolsSurfaceHost : IDevToolsSurfaceHost
{
    private readonly MainWindow _window;
    private bool _isClosed;
    private bool _isShown;

    public DesktopDevToolsSurfaceHost(
        IDevToolsRootSource rootSource,
        DevToolsOptions options,
        Application application)
    {
        _window = new MainWindow
        {
            Root = new ApplicationRootNode(rootSource, application),
            Width = options.Size.Width,
            Height = options.Size.Height,
        };
        _window.SetOptions(options);
        _window.Closed += HandleWindowClosed;
    }

    public event EventHandler? Closed;

    public DevToolsHostKind Kind => DevToolsHostKind.DesktopWindow;

    public void ShowOrActivate(Control? focusedControl)
    {
        if (_isClosed)
        {
            return;
        }

        _window.SelectedControl(focusedControl);
        if (_isShown)
        {
            _window.Activate();
            return;
        }

        _isShown = true;
        _window.Show();
    }

    public void Close()
    {
        if (!_isClosed)
        {
            _window.Close();
        }
    }

    public void Dispose()
    {
        if (!_isClosed)
        {
            _window.Close();
        }

        _window.Closed -= HandleWindowClosed;
    }

    private void HandleWindowClosed(object? sender, EventArgs e)
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        Closed?.Invoke(this, EventArgs.Empty);
    }
}

internal sealed class EmbeddedDevToolsSurfaceHost : IDevToolsSurfaceHost
{
    private readonly Application _application;
    private readonly DevToolsOptions _options;
    private readonly SingleViewApplicationRootSource _rootSource;
    private EmbeddedDevToolsView? _devToolsView;
    private Grid? _fallbackRoot;
    private Control? _originalMainView;
    private OverlayLayer? _overlayLayer;
    private MainViewModel? _viewModel;
    private bool _isDisposed;
    private bool _isOpen;

    public EmbeddedDevToolsSurfaceHost(
        SingleViewApplicationRootSource rootSource,
        DevToolsOptions options,
        Application application)
    {
        _rootSource = rootSource;
        _options = options;
        _application = application;
    }

    public event EventHandler? Closed;

    public DevToolsHostKind Kind => DevToolsHostKind.EmbeddedSingleView;

    public void ShowOrActivate(Control? focusedControl)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_isOpen)
        {
            Close();
            return;
        }

        Open(focusedControl);
    }

    public void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        ClearSurface();

        Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Close();
        _isDisposed = true;
    }

    private void ClearSurface()
    {
        var viewModel = _viewModel;

        if (_overlayLayer is not null && _devToolsView is not null)
        {
            _overlayLayer.Children.Remove(_devToolsView);
        }

        if (_fallbackRoot is not null)
        {
            if (_devToolsView is not null)
            {
                _fallbackRoot.Children.Remove(_devToolsView);
            }

            if (_originalMainView is not null)
            {
                _fallbackRoot.Children.Remove(_originalMainView);
                if (_rootSource.Lifetime.MainView is null || ReferenceEquals(_rootSource.Lifetime.MainView, _fallbackRoot))
                {
                    _rootSource.Lifetime.MainView = _originalMainView;
                }
            }
        }

        if (_devToolsView is not null)
        {
            _devToolsView.DataContext = null;
        }

        _viewModel = null;
        _devToolsView = null;
        _overlayLayer = null;
        _fallbackRoot = null;
        _originalMainView = null;
        viewModel?.Dispose();
    }

    private void Open(Control? focusedControl)
    {
        _originalMainView = _rootSource.Lifetime.MainView
            ?? throw new InvalidOperationException("The single-view application does not have a MainView to host DevScope over.");

        try
        {
            _viewModel = new MainViewModel(new ApplicationRootNode(_rootSource, _application), Close);
            _viewModel.SetOptions(_options);
            if (focusedControl is not null)
            {
                _viewModel.SelectControl(focusedControl);
            }

            _devToolsView = new EmbeddedDevToolsView(_viewModel);
            if (_options.ThemeVariant is { } themeVariant)
            {
                _devToolsView.SetCurrentValue(ThemeVariantScope.RequestedThemeVariantProperty, themeVariant);
            }

            _overlayLayer = OverlayLayer.GetOverlayLayer(_originalMainView);
            if (_overlayLayer is not null)
            {
                _overlayLayer.Children.Add(_devToolsView);
            }
            else
            {
                _fallbackRoot = new Grid();
                _rootSource.Lifetime.MainView = null;
                _fallbackRoot.Children.Add(_originalMainView);
                _fallbackRoot.Children.Add(_devToolsView);
                _rootSource.Lifetime.MainView = _fallbackRoot;
            }

            _isOpen = true;
        }
        catch
        {
            ClearSurface();
            throw;
        }
    }
}
