using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Raw;
using Application = Avalonia.Application;

namespace ClassicDiagnostics.Avalonia.Hosting;

internal sealed class DevToolsApplicationSession : IDisposable
{
    private readonly Application _application;
    private readonly IClassicDesktopStyleApplicationLifetime _lifetime;
    private readonly DevToolsOptions _options;
    private readonly DevToolsWindowManager _windowManager;
    private readonly SerialDisposableValue _openedWindow = new();
    private readonly IDisposable? _preProcessSubscription;
    private bool _isDisposed;

    public DevToolsApplicationSession(
        Application application,
        DevToolsOptions options,
        DevToolsWindowManager windowManager)
    {
        _application = application;
        _options = options;
        _windowManager = windowManager;

        if (_application.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            throw new ArgumentNullException(
                nameof(application),
                "DevTools can only attach to applications that support IClassicDesktopStyleApplicationLifetime.");
        }

        _lifetime = lifetime;

        // The input manager belongs to the application lifetime. This session owns the
        // subscription so AttachDevTools() callers can detach without leaving F12 handlers alive.
        _preProcessSubscription = _application.InputManager?.PreProcess.Subscribe(OnPreProcess);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _preProcessSubscription?.Dispose();
        _openedWindow.Dispose();
        _isDisposed = true;
    }

    private void OnPreProcess(RawInputEventArgs e)
    {
        if (e is not RawKeyEventArgs keyEventArgs ||
            keyEventArgs.Type != RawKeyEventType.KeyUp ||
            !_options.Gesture.Matches(keyEventArgs))
        {
            return;
        }

        var owner = _lifetime.MainWindow;
        _openedWindow.Disposable = _windowManager.Open(
            new ClassicDesktopStyleApplicationLifetimeTopLevelGroup(_lifetime),
            _options,
            owner,
            _application);
        e.Handled = true;
    }
    private sealed class SerialDisposableValue : IDisposable
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
}
