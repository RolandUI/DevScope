using System.Diagnostics;
using System.Globalization;

namespace RolandUI.DevScope.Tracing;

internal sealed class SystemTraceCaptureSource : ITraceCaptureSource
{
    private readonly DevScopeTraceListener _listener;
    private int _isDisposed;

    public SystemTraceCaptureSource()
    {
        _listener = new DevScopeTraceListener(this);
        Trace.Listeners.Add(_listener);
    }

    public event Action<TraceEntry>? EntryReceived;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
        {
            return;
        }

        Trace.Listeners.Remove(_listener);
        _listener.Dispose();
        EntryReceived = null;
    }

    private void Publish(TraceEventType severity, string? category, string? message)
    {
        if (Volatile.Read(ref _isDisposed) != 0 || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        EntryReceived?.Invoke(new TraceEntry(
            DateTimeOffset.Now,
            severity,
            string.IsNullOrWhiteSpace(category) ? "Trace" : category,
            message));
    }

    private sealed class DevScopeTraceListener(SystemTraceCaptureSource owner) : TraceListener("DevScope")
    {
        public override void Write(string? message)
        {
            owner.Publish(TraceEventType.Information, "Trace", message);
        }

        public override void WriteLine(string? message)
        {
            owner.Publish(TraceEventType.Information, "Trace", message);
        }

        public override void WriteLine(string? message, string? category)
        {
            owner.Publish(TraceEventType.Information, category, message);
        }

        public override void Fail(string? message, string? detailMessage)
        {
            var combined = string.IsNullOrWhiteSpace(detailMessage)
                ? message
                : $"{message}{Environment.NewLine}{detailMessage}";
            owner.Publish(TraceEventType.Error, "Assert", combined);
        }

        public override void TraceEvent(
            TraceEventCache? eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string? message)
        {
            owner.Publish(eventType, source, message);
        }

        public override void TraceEvent(
            TraceEventCache? eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string? format,
            params object?[]? args)
        {
            var message = args is { Length: > 0 }
                ? string.Format(CultureInfo.InvariantCulture, format ?? string.Empty, args)
                : format;
            owner.Publish(eventType, source, message);
        }

        public override void TraceData(
            TraceEventCache? eventCache,
            string source,
            TraceEventType eventType,
            int id,
            object? data)
        {
            owner.Publish(eventType, source, data?.ToString());
        }

        public override void TraceData(
            TraceEventCache? eventCache,
            string source,
            TraceEventType eventType,
            int id,
            params object?[]? data)
        {
            owner.Publish(
                eventType,
                source,
                data is null ? null : string.Join(", ", data.Select(value => value?.ToString())));
        }
    }
}
