using System.Diagnostics;

namespace RolandUI.DevScope.Tracing;

internal sealed record TraceEntry(
    DateTimeOffset Timestamp,
    TraceEventType Severity,
    string Category,
    string Message);
