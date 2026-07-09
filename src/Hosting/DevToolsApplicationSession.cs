using Avalonia.Input.Raw;
using Application = Avalonia.Application;

namespace RolandUI.DevScope.Hosting;

internal sealed class DevToolsApplicationSession : IDisposable
{
    private readonly Application _application;
    private readonly DevToolsOptions _options;
    private readonly IDevToolsRootSource _rootSource;
    private readonly IDisposable? _preProcessSubscription;
    private bool _isDisposed;

    public DevToolsApplicationSession(Application application, DevToolsOptions options)
    {
        _application = application;
        _options = options;
        _rootSource = DevToolsRootSources.Create(_application);

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
        _isDisposed = true;
    }

    private void HandlePreProcess(RawInputEventArgs e)
    {
        if (e is not RawKeyEventArgs { Type: RawKeyEventType.KeyUp } keyEventArgs || !_options.Gesture.Matches(keyEventArgs))
        {
            return;
        }

        DevToolsWindowHost.ShowOrActivate(_rootSource, _options, _application);
        e.Handled = true;
    }
}