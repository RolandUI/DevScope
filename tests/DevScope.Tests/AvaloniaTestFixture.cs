using Avalonia;
using Avalonia.Threading;
using RolandUI.DevScope.Elements;
using RolandUI.DevScope.Elements.Properties.Models;
using RolandUI.DevScope.Elements.Properties.Services;
using RolandUI.DevScope.Elements.Properties.ViewModels;
using RolandUI.DevScope.Elements.Trees;
using RolandUI.DevScope.Rooting;
using RolandUI.DevScope.Shell;

namespace RolandUI.DevScope.Tests;

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
