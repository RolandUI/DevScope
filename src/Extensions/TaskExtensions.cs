namespace RolandUI.DevScope.Extensions;

internal static class TaskExtensions
{
    /// <summary>
    /// Runs fire-and-forget work from sync Avalonia command entry points while keeping
    /// asynchronous failures connected to the DevTools error channel.
    /// </summary>
    public static void Detach(this Task task, string message, IDevToolsErrorSink? errorSink = null)
    {
        _ = WatchAsync(task, message, errorSink ?? DevToolsDiagnostics.ErrorSink);
    }

    private static async Task WatchAsync(Task task, string message, IDevToolsErrorSink errorSink)
    {
        try
        {
            await task;
        }
        catch (Exception exception)
        {
            errorSink.Report(exception, message);
        }
    }
}
