using System.Diagnostics;

namespace RolandUI.DevScope;

internal interface IDevToolsErrorSink
{
    void Report(Exception exception, string message);
}

internal static class DevToolsDiagnostics
{
    public static IDevToolsErrorSink ErrorSink
    {
        get => field ??= TraceDevToolsErrorSink.Instance;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static void Report(Exception exception, string message)
    {
        ErrorSink.Report(exception, message);
    }

    private sealed class TraceDevToolsErrorSink : IDevToolsErrorSink
    {
        public static TraceDevToolsErrorSink Instance { get; } = new();

        void IDevToolsErrorSink.Report(Exception exception, string message)
        {
            Trace.TraceError(
                "[RolandUI.DevScope] {0}{1}{2}",
                message,
                Environment.NewLine,
                exception);
        }
    }
}