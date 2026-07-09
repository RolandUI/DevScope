using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Animation;
using Avalonia.Data;

namespace RolandUI.DevScope.AnimationTiming;

internal readonly record struct AnimationClockCompatibility(bool IsSupported, string Diagnostic);

internal interface IAnimationClockAdapter
{
    AnimationClockCompatibility Compatibility { get; }

    bool TryAttach(
        Animatable target,
        out IAnimationClockSession? session,
        out string diagnostic);
}

internal interface IAnimationClockSession : IDisposable
{
    event EventHandler? Changed;

    Animatable Target { get; }

    TimeSpan CurrentTime { get; }

    bool IsPaused { get; }

    void Pause();

    void Resume();

    void Advance(TimeSpan amount);

    void Reset();
}

internal interface IAnimationTimeSource
{
    IDisposable Subscribe(Action<TimeSpan> tick);
}

/// <summary>
/// Isolates the Avalonia 12.1 internal animation-clock surface used by the experimental
/// diagnostics controls. Keep all direct IClock and Animatable.ClockProperty access here.
/// </summary>
internal sealed class AvaloniaAnimationClockAdapter : IAnimationClockAdapter
{
    public static AvaloniaAnimationClockAdapter Instance { get; } = new();

    private readonly Func<IAnimationTimeSource> _timeSourceFactory;

    public AvaloniaAnimationClockAdapter()
        : this(ProbeCompatibility(), static () => new AvaloniaAnimationTimeSource())
    {
    }

    internal AvaloniaAnimationClockAdapter(
        AnimationClockCompatibility compatibility,
        Func<IAnimationTimeSource> timeSourceFactory)
    {
        Compatibility = compatibility;
        _timeSourceFactory = timeSourceFactory ?? throw new ArgumentNullException(nameof(timeSourceFactory));
    }

    public AnimationClockCompatibility Compatibility { get; }

    public bool TryAttach(
        Animatable target,
        out IAnimationClockSession? session,
        out string diagnostic)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (!Compatibility.IsSupported)
        {
            session = null;
            diagnostic = Compatibility.Diagnostic;
            return false;
        }

        try
        {
            target.Dispatcher.VerifyAccess();
            session = CreateSession(target, _timeSourceFactory());
            diagnostic = $"Experimental clock attached to {target.GetType().Name}.";
            return true;
        }
        catch (Exception error)
        {
            session = null;
            diagnostic =
                $"Experimental animation clock is unavailable for this target: {error.GetType().Name}: {error.Message}";
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IAnimationClockSession CreateSession(Animatable target, IAnimationTimeSource timeSource)
    {
        var clock = new DiagnosticClock(timeSource);
        try
        {
            var clockOverride = target.SetValue(
                    Animatable.ClockProperty,
                    clock,
                    BindingPriority.Animation)
                ?? throw new InvalidOperationException("Avalonia did not return a reversible clock property override.");

            return new AnimationClockSession(target, clock, clockOverride);
        }
        catch
        {
            clock.Release();
            throw;
        }
    }

    private static AnimationClockCompatibility ProbeCompatibility()
    {
        var assembly = typeof(Animatable).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+')[0]
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        try
        {
            var clockInterface = assembly.GetType("Avalonia.Animation.IClock", throwOnError: false);
            var clockBase = assembly.GetType("Avalonia.Animation.ClockBase", throwOnError: false);
            var clock = assembly.GetType("Avalonia.Animation.Clock", throwOnError: false);
            var clockPropertyField = typeof(Animatable).GetField(
                "ClockProperty",
                BindingFlags.Static | BindingFlags.NonPublic);
            var clockProperty = typeof(Animatable).GetProperty(
                "Clock",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (clockInterface is null || clockBase is null || clock is null)
            {
                return Unsupported(version, "IClock, ClockBase, or Clock was not found");
            }

            if (!typeof(IObservable<TimeSpan>).IsAssignableFrom(clockInterface)
                || clockInterface.GetProperty(nameof(IClock.PlayState))?.PropertyType != typeof(PlayState))
            {
                return Unsupported(version, "IClock no longer exposes the expected observable PlayState contract");
            }

            if (!clockInterface.IsAssignableFrom(clockBase)
                || clockBase.GetMethod(
                    "Pulse",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    types: [typeof(TimeSpan)],
                    modifiers: null) is null)
            {
                return Unsupported(version, "ClockBase no longer exposes the expected Pulse(TimeSpan) shape");
            }

            if (clock.GetProperty("GlobalClock", BindingFlags.Static | BindingFlags.Public)?.PropertyType != clockInterface)
            {
                return Unsupported(version, "Clock.GlobalClock no longer exposes IClock");
            }

            if (clockPropertyField?.GetValue(null) is not AvaloniaProperty property
                || !property.Inherits
                || clockProperty?.CanRead != true
                || clockProperty.CanWrite != true
                || clockProperty.PropertyType != clockInterface)
            {
                return Unsupported(version, "Animatable.ClockProperty no longer has the expected inheritable IClock shape");
            }

            return new AnimationClockCompatibility(
                true,
                $"Avalonia {version} internal animation clock API detected. This feature is experimental and version-sensitive.");
        }
        catch (Exception error)
        {
            return Unsupported(version, $"API inspection failed: {error.GetType().Name}: {error.Message}");
        }
    }

    private static AnimationClockCompatibility Unsupported(string version, string reason)
    {
        return new AnimationClockCompatibility(
            false,
            $"Experimental animation clock controls are unavailable for Avalonia {version}: {reason}.");
    }

    private sealed class AvaloniaAnimationTimeSource : IAnimationTimeSource
    {
        public IDisposable Subscribe(Action<TimeSpan> tick)
        {
            return Clock.GlobalClock.Subscribe(tick);
        }
    }

    private sealed class AnimationClockSession : IAnimationClockSession
    {
        private readonly DiagnosticClock _clock;
        private readonly IDisposable _clockOverride;
        private bool _isDisposed;

        public AnimationClockSession(
            Animatable target,
            DiagnosticClock clock,
            IDisposable clockOverride)
        {
            Target = target;
            _clock = clock;
            _clockOverride = clockOverride;
            _clock.Changed += HandleClockChanged;
        }

        public event EventHandler? Changed;

        public Animatable Target { get; }

        public TimeSpan CurrentTime => _clock.CurrentTime;

        public bool IsPaused => _clock.PlayState == PlayState.Pause;

        public void Pause()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _clock.Pause();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Resume()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _clock.Resume();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Advance(TimeSpan amount)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _clock.Advance(amount);
        }

        public void Reset()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _clock.Reset();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _clock.Changed -= HandleClockChanged;
            try
            {
                _clockOverride.Dispose();
            }
            finally
            {
                _clock.Release();
                _isDisposed = true;
            }
        }

        private void HandleClockChanged(object? sender, EventArgs e)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class DiagnosticClock : IClock
    {
        private readonly List<IObserver<TimeSpan>> _observers = [];
        private readonly IDisposable _timeSubscription;
        private TimeSpan? _lastSourceTime;
        private bool _isReleased;
        private bool _isSourceDisposed;

        public DiagnosticClock(IAnimationTimeSource timeSource)
        {
            ArgumentNullException.ThrowIfNull(timeSource);
            _timeSubscription = timeSource.Subscribe(HandleSourceTick);
        }

        public event EventHandler? Changed;

        public TimeSpan CurrentTime { get; private set; }

        public PlayState PlayState { get; set; } = PlayState.Run;

        public IDisposable Subscribe(IObserver<TimeSpan> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            ObjectDisposedException.ThrowIf(_isSourceDisposed, this);

            _observers.Add(observer);
            try
            {
                observer.OnNext(CurrentTime);
            }
            catch
            {
                _observers.Remove(observer);
                throw;
            }

            return Disposable.Create(() =>
            {
                _observers.Remove(observer);
                TryDisposeReleasedClock();
            });
        }

        public void Pause()
        {
            PlayState = PlayState.Pause;
            _lastSourceTime = null;
        }

        public void Resume()
        {
            PlayState = PlayState.Run;
            _lastSourceTime = null;
        }

        public void Advance(TimeSpan amount)
        {
            if (amount <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Animation clock steps must be positive.");
            }

            CurrentTime += amount;
            PublishCurrentTime();
        }

        public void Reset()
        {
            CurrentTime = TimeSpan.Zero;
            _lastSourceTime = null;
            PublishCurrentTime();
        }

        public void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            PlayState = PlayState.Run;
            _lastSourceTime = null;
            TryDisposeReleasedClock();
        }

        private void HandleSourceTick(TimeSpan sourceTime)
        {
            if (_isSourceDisposed)
            {
                return;
            }

            var previous = _lastSourceTime;
            _lastSourceTime = sourceTime;
            if (previous is null || (!_isReleased && PlayState != PlayState.Run))
            {
                return;
            }

            var elapsed = sourceTime - previous.Value;
            if (elapsed <= TimeSpan.Zero)
            {
                return;
            }

            CurrentTime += elapsed;
            PublishCurrentTime();
        }

        private void PublishCurrentTime()
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnNext(CurrentTime);
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void TryDisposeReleasedClock()
        {
            if (!_isReleased || _observers.Count != 0 || _isSourceDisposed)
            {
                return;
            }

            _isSourceDisposed = true;
            _timeSubscription.Dispose();
        }
    }
}
