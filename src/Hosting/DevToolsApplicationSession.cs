using System.Runtime.CompilerServices;
using Avalonia.Input.Raw;
using Application = Avalonia.Application;

namespace RolandUI.DevScope.Hosting;

internal sealed class DevToolsApplicationSession : IDisposable
{
    private static readonly DevToolsApplicationSessionRegistry Registry = new();
    private readonly Application _application;
    private readonly DevToolsHostManager _hostManager;
    private readonly DevToolsOptions _options;
    private readonly IDevToolsRootSource _rootSource;
    private readonly IDisposable? _preProcessSubscription;
    private bool _isDisposed;

    internal static IDisposable Attach(Application application, DevToolsOptions options)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(options);

        return Registry.Acquire(application, () => new DevToolsApplicationSession(application, options));
    }

    private DevToolsApplicationSession(Application application, DevToolsOptions options)
    {
        _application = application;
        _options = options;
        _rootSource = DevToolsRootSources.Create(_application);
        _hostManager = new DevToolsHostManager(_rootSource, _options, _application);

        // The input manager belongs to the application lifetime. This session owns the
        // subscription so AttachDevTools() callers can detach without leaving F12 handlers alive.
        _preProcessSubscription = _application.InputManager?.PreProcess.Subscribe(HandlePreProcess);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _preProcessSubscription?.Dispose();
        _hostManager.Dispose();
        _isDisposed = true;
    }

    private void HandlePreProcess(RawInputEventArgs e)
    {
        if (e is not RawKeyEventArgs { Type: RawKeyEventType.KeyUp } keyEventArgs || !_options.Gesture.Matches(keyEventArgs))
        {
            return;
        }

        _hostManager.ShowOrActivate();
        e.Handled = true;
    }
}

internal sealed class DevToolsApplicationSessionRegistry
{
    private readonly ConditionalWeakTable<Application, Entry> _entries = new();
    private readonly object _sync = new();

    public IDisposable Acquire(Application application, Func<IDisposable> sessionFactory)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(sessionFactory);

        Entry entry;
        lock (_sync)
        {
            if (!_entries.TryGetValue(application, out entry!))
            {
                entry = new Entry(sessionFactory());
                _entries.Add(application, entry);
            }

            entry.ReferenceCount++;
        }

        return new Lease(this, application, entry);
    }

    private void Release(Application application, Entry entry)
    {
        IDisposable? sessionToDispose = null;
        lock (_sync)
        {
            if (!_entries.TryGetValue(application, out var current) || !ReferenceEquals(current, entry))
            {
                return;
            }

            current.ReferenceCount--;
            if (current.ReferenceCount == 0)
            {
                _entries.Remove(application);
                sessionToDispose = current.Session;
            }
        }

        sessionToDispose?.Dispose();
    }

    private sealed class Entry(IDisposable session)
    {
        public IDisposable Session { get; } = session ?? throw new ArgumentNullException(nameof(session));

        public int ReferenceCount { get; set; }
    }

    private sealed class Lease(
        DevToolsApplicationSessionRegistry registry,
        Application application,
        Entry entry
    ) : IDisposable
    {
        private DevToolsApplicationSessionRegistry? _registry = registry;

        public void Dispose()
        {
            Interlocked.Exchange(ref _registry, null)?.Release(application, entry);
        }
    }
}
