using Avalonia;
using Avalonia.Threading;

namespace ClassicDiagnostics.Avalonia.Tests;

internal static class AvaloniaTestFixture
{
    private static readonly object Sync = new();
    private static bool _isInitialized;

    public static void RunOnUIThread(Action action)
    {
        EnsureInitialized();

        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Invoke(action);
    }

    private static void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        lock (Sync)
        {
            if (_isInitialized)
            {
                return;
            }

            if (Application.Current is null)
            {
                AvaloniaHeadlessTestApp.BuildAvaloniaApp().SetupWithoutStarting();
            }

            _isInitialized = true;
        }
    }
}
