using Avalonia;
using Avalonia.Threading;
using ClassicDiagnostics.Avalonia.Elements;
using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;
using ClassicDiagnostics.Avalonia.Elements.Trees;
using ClassicDiagnostics.Avalonia.Rooting;
using ClassicDiagnostics.Avalonia.Shell;

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
