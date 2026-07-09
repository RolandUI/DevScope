namespace RolandUI.DevScope.Tracing;

internal interface ITraceCaptureSource : IDisposable
{
    event Action<TraceEntry>? EntryReceived;
}
