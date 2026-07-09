using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Animation;
using Avalonia.Threading;
using RolandUI.DevScope.AnimationTiming;
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
    private readonly IAnimationClockAdapter _animationClockAdapter;
    private readonly Func<Animatable?> _selectedAnimationTarget;
    private IAnimationClockSession? _animationClockSession;
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

    internal TracePageViewModel(Func<Animatable?> selectedAnimationTarget)
        : this(
            new SystemTraceCaptureSource(),
            action => Dispatcher.UIThread.Post(action, DispatcherPriority.Background),
            selectedAnimationTarget: selectedAnimationTarget)
    {
    }

    internal TracePageViewModel(
        ITraceCaptureSource source,
        Action<Action> postToUi,
        int bufferLimit = DefaultBufferLimit,
        int drainBatchSize = DefaultDrainBatchSize,
        Func<Animatable?>? selectedAnimationTarget = null,
        IAnimationClockAdapter? animationClockAdapter = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(drainBatchSize);

        _source = source;
        _postToUi = postToUi;
        _selectedAnimationTarget = selectedAnimationTarget ?? (static () => null);
        _animationClockAdapter = animationClockAdapter ?? AvaloniaAnimationClockAdapter.Instance;
        _bufferLimit = bufferLimit;
        _drainBatchSize = drainBatchSize;
        _source.EntryReceived += HandleEntryReceived;
        AnimationClockStatus = _animationClockAdapter.Compatibility.Diagnostic;
        UpdateAnimationClockPresentation();
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

    public bool IsAnimationClockSupported => _animationClockAdapter.Compatibility.IsSupported;

    public bool IsAnimationClockAttached
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public bool IsAnimationClockPaused
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public decimal? AnimationClockStepMilliseconds
    {
        get;
        set => SetProperty(ref field, value);
    } = 16.667m;

    public string AnimationClockTarget
    {
        get;
        private set => SetProperty(ref field, value);
    } = "No target attached";

    public string AnimationClockState
    {
        get;
        private set => SetProperty(ref field, value);
    } = "Detached";

    public string AnimationClockTime
    {
        get;
        private set => SetProperty(ref field, value);
    } = "0.000 s";

    public string AnimationClockStatus
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public void AttachAnimationClock()
    {
        if (!IsAnimationClockSupported)
        {
            AnimationClockStatus = _animationClockAdapter.Compatibility.Diagnostic;
            return;
        }

        var target = _selectedAnimationTarget();
        if (target is null)
        {
            AnimationClockStatus = "Select an animatable control in the Elements tree before attaching the clock.";
            return;
        }

        if (_animationClockSession is { Target: var currentTarget } && ReferenceEquals(currentTarget, target))
        {
            AnimationClockStatus = $"The experimental clock is already attached to {target.GetType().Name}.";
            return;
        }

        DetachAnimationClockCore(updateStatus: false);
        if (!_animationClockAdapter.TryAttach(target, out var session, out var diagnostic) || session is null)
        {
            AnimationClockStatus = diagnostic;
            UpdateAnimationClockPresentation();
            return;
        }

        _animationClockSession = session;
        _animationClockSession.Changed += HandleAnimationClockChanged;
        AnimationClockStatus =
            $"{diagnostic} Start or restart target animations while attached so they use the diagnostic clock.";
        UpdateAnimationClockPresentation();
    }

    public void PauseAnimationClock()
    {
        RunAnimationClockAction(
            static session => session.Pause(),
            "Diagnostic animation time is paused. DevScope remains live.");
    }

    public void ResumeAnimationClock()
    {
        RunAnimationClockAction(
            static session => session.Resume(),
            "Diagnostic animation time is following the application render clock.");
    }

    public void StepAnimationClock()
    {
        if (AnimationClockStepMilliseconds is not > 0m)
        {
            AnimationClockStatus = "Step size must be greater than zero milliseconds.";
            return;
        }

        var amount = TimeSpan.FromMilliseconds((double)AnimationClockStepMilliseconds.Value);
        RunAnimationClockAction(
            session => session.Advance(amount),
            $"Advanced diagnostic animation time by {amount.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)} ms.");
    }

    public void ResetAnimationClock()
    {
        RunAnimationClockAction(
            static session => session.Reset(),
            "Diagnostic animation time reset to zero.");
    }

    public void DetachAnimationClock()
    {
        DetachAnimationClockCore(updateStatus: true);
    }

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
        DetachAnimationClockCore(updateStatus: false);
        _source.Dispose();
        Interlocked.Increment(ref _generation);
        while (_pending.TryDequeue(out _))
        {
        }

        base.Dispose(disposing);
    }

    private void RunAnimationClockAction(Action<IAnimationClockSession> action, string successStatus)
    {
        if (_animationClockSession is not { } session)
        {
            AnimationClockStatus = "Attach the experimental clock to the selected target first.";
            return;
        }

        try
        {
            action(session);
            AnimationClockStatus = successStatus;
            UpdateAnimationClockPresentation();
        }
        catch (Exception error)
        {
            AnimationClockStatus = $"Animation clock command failed: {error.GetType().Name}: {error.Message}";
        }
    }

    private void DetachAnimationClockCore(bool updateStatus)
    {
        Exception? detachError = null;
        if (_animationClockSession is { } session)
        {
            session.Changed -= HandleAnimationClockChanged;
            _animationClockSession = null;
            try
            {
                session.Dispose();
            }
            catch (Exception error)
            {
                detachError = error;
            }
        }

        if (updateStatus)
        {
            AnimationClockStatus = detachError is null
                ? "Experimental clock detached. The previous target clock value and normal animation flow were restored."
                : $"Animation clock detach reported {detachError.GetType().Name}: {detachError.Message}";
        }

        UpdateAnimationClockPresentation();
    }

    private void HandleAnimationClockChanged(object? sender, EventArgs e)
    {
        UpdateAnimationClockPresentation();
    }

    private void UpdateAnimationClockPresentation()
    {
        var session = _animationClockSession;
        IsAnimationClockAttached = session is not null;
        IsAnimationClockPaused = session?.IsPaused == true;
        AnimationClockTarget = session?.Target.GetType().Name ?? "No target attached";
        AnimationClockState = session is null
            ? IsAnimationClockSupported ? "Detached" : "Unavailable"
            : session.IsPaused ? "Paused" : "Running";
        AnimationClockTime =
            (session?.CurrentTime.TotalSeconds ?? 0).ToString("0.000", CultureInfo.InvariantCulture) + " s";
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
