using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using RolandUI.DevScope.Tracing;

namespace RolandUI.DevScope.ViewModels;

internal sealed class TracePageViewModel : ReactiveViewModelBase
{
    internal const int DefaultBufferLimit = 1_000;
    private const int DefaultDrainBatchSize = 256;

    private readonly Queue<TraceEntry> _buffer = new();
    private readonly int _bufferLimit;
    private readonly int _drainBatchSize;
    private readonly ConcurrentQueue<PendingTraceEntry> _pending = new();
    private readonly Action<Action> _postToUi;
    private readonly ITraceCaptureSource _source;
    private int _capturePaused;
    private int _drainScheduled;
    private int _generation;
    private int _isDisposed;

    public TracePageViewModel()
        : this(
            new SystemTraceCaptureSource(),
            action => Dispatcher.UIThread.Post(action, DispatcherPriority.Background))
    {
    }

    internal TracePageViewModel(
        ITraceCaptureSource source,
        Action<Action> postToUi,
        int bufferLimit = DefaultBufferLimit,
        int drainBatchSize = DefaultDrainBatchSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(drainBatchSize);

        _source = source;
        _postToUi = postToUi;
        _bufferLimit = bufferLimit;
        _drainBatchSize = drainBatchSize;
        _source.EntryReceived += HandleEntryReceived;
        UpdateStatus();
    }

    public ObservableCollection<TraceEntry> Entries { get; } = [];

    public string FilterText
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? string.Empty))
            {
                RefreshFilteredEntries();
            }
        }
    } = string.Empty;

    public bool IsPaused
    {
        get => Volatile.Read(ref _capturePaused) != 0;
        set
        {
            var newValue = value ? 1 : 0;
            if (Interlocked.Exchange(ref _capturePaused, newValue) != newValue)
            {
                RaisePropertyChanged();
                UpdateStatus();
            }
        }
    }

    public bool AutoScroll
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public string Status
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public void Clear()
    {
        Interlocked.Increment(ref _generation);
        while (_pending.TryDequeue(out _))
        {
        }

        _buffer.Clear();
        Entries.Clear();
        UpdateStatus();
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing || Interlocked.Exchange(ref _isDisposed, 1) != 0)
        {
            base.Dispose(disposing);
            return;
        }

        _source.EntryReceived -= HandleEntryReceived;
        _source.Dispose();
        Interlocked.Increment(ref _generation);
        while (_pending.TryDequeue(out _))
        {
        }

        base.Dispose(disposing);
    }

    private void HandleEntryReceived(TraceEntry entry)
    {
        if (Volatile.Read(ref _isDisposed) != 0 || Volatile.Read(ref _capturePaused) != 0)
        {
            return;
        }

        _pending.Enqueue(new PendingTraceEntry(Volatile.Read(ref _generation), entry));
        ScheduleDrain();
    }

    private void ScheduleDrain()
    {
        if (Interlocked.CompareExchange(ref _drainScheduled, 1, 0) != 0)
        {
            return;
        }

        try
        {
            _postToUi(DrainPending);
        }
        catch
        {
            Interlocked.Exchange(ref _drainScheduled, 0);
        }
    }

    private void DrainPending()
    {
        try
        {
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                return;
            }

            var generation = Volatile.Read(ref _generation);
            var processed = 0;

            while (processed < _drainBatchSize && _pending.TryDequeue(out var pending))
            {
                if (pending.Generation == generation)
                {
                    AddEntry(pending.Entry);
                }

                processed++;
            }

            UpdateStatus();
        }
        finally
        {
            Interlocked.Exchange(ref _drainScheduled, 0);
            if (Volatile.Read(ref _isDisposed) == 0 && !_pending.IsEmpty)
            {
                ScheduleDrain();
            }
        }
    }

    private void AddEntry(TraceEntry entry)
    {
        _buffer.Enqueue(entry);
        if (MatchesFilter(entry))
        {
            Entries.Add(entry);
        }

        if (_buffer.Count <= _bufferLimit)
        {
            return;
        }

        var removed = _buffer.Dequeue();
        if (MatchesFilter(removed) && Entries.Count > 0 && ReferenceEquals(Entries[0], removed))
        {
            Entries.RemoveAt(0);
        }
    }

    private void RefreshFilteredEntries()
    {
        Entries.Clear();
        foreach (var entry in _buffer.Where(MatchesFilter))
        {
            Entries.Add(entry);
        }

        UpdateStatus();
    }

    private bool MatchesFilter(TraceEntry entry)
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            return true;
        }

        return entry.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
            || entry.Category.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
            || entry.Severity.ToString().Contains(FilterText, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateStatus()
    {
        var prefix = IsPaused ? "Capture paused. " : string.Empty;
        Status = $"{prefix}{Entries.Count} of {_buffer.Count} buffered entries shown (limit {_bufferLimit}).";
    }

    private readonly record struct PendingTraceEntry(int Generation, TraceEntry Entry);
}
