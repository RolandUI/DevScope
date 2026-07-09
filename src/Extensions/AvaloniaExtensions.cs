using Avalonia.Threading;

namespace RolandUI.DevScope.Extensions;

public static class AvaloniaExtensions
{
    public static void PostOnDemand(this Dispatcher dispatcher, Action action, DispatcherPriority priority = default)
    {
        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Post(action, priority);
        }
    }
}