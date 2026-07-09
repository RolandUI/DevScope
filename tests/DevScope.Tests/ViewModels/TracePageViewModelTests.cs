using System.Diagnostics;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RolandUI.DevScope.AnimationTiming;
using RolandUI.DevScope.Tracing;
using RolandUI.DevScope.ViewModels;
using RolandUI.DevScope.Views;

namespace RolandUI.DevScope.Tests.ViewModels;

internal sealed class TracePageViewModelTests
{
    [Test]
    public async Task BackgroundEntriesArePostedBeforeUpdatingUiCollection()
    {
        var source = new FakeTraceCaptureSource();
        var dispatcher = new ManualUiDispatcher();
        using var viewModel = new TracePageViewModel(source, dispatcher.Post);

        await Task.Run(() => source.Emit(CreateEntry("background")));

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Entries, Is.Empty);
            Assert.That(dispatcher.PendingCount, Is.EqualTo(1));
        });

        dispatcher.RunAll();

        Assert.That(viewModel.Entries.Select(entry => entry.Message), Is.EqualTo(new[] { "background" }));
    }

    [Test]
    public void BurstyInputIsBatchedAndBounded()
    {
        var source = new FakeTraceCaptureSource();
        var dispatcher = new ManualUiDispatcher();
        using var viewModel = new TracePageViewModel(source, dispatcher.Post, bufferLimit: 3, drainBatchSize: 2);

        for (var index = 0; index < 10_000; index++)
        {
            source.Emit(CreateEntry($"entry-{index}"));
        }

        Assert.That(dispatcher.PendingCount, Is.EqualTo(1), "A burst should coalesce into one scheduled drain.");

        dispatcher.RunAll();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Entries.Select(entry => entry.Message), Is.EqualTo(new[]
            {
                "entry-9997",
                "entry-9998",
                "entry-9999",
            }));
            Assert.That(viewModel.Status, Does.Contain("3 of 3"));
        });
    }

    [Test]
    public void FilteringMatchesMessageCategoryAndSeverity()
    {
        var source = new FakeTraceCaptureSource();
        var dispatcher = new ManualUiDispatcher();
        using var viewModel = new TracePageViewModel(source, dispatcher.Post);

        source.Emit(CreateEntry("arranged", TraceEventType.Information, "Layout"));
        source.Emit(CreateEntry("request failed", TraceEventType.Error, "Network"));
        source.Emit(CreateEntry("rendered", TraceEventType.Verbose, "Rendering"));
        dispatcher.RunAll();

        viewModel.FilterText = "layout";
        Assert.That(viewModel.Entries.Select(entry => entry.Message), Is.EqualTo(new[] { "arranged" }));

        viewModel.FilterText = "error";
        Assert.That(viewModel.Entries.Select(entry => entry.Message), Is.EqualTo(new[] { "request failed" }));

        viewModel.FilterText = "rendered";
        Assert.That(viewModel.Entries.Select(entry => entry.Category), Is.EqualTo(new[] { "Rendering" }));
    }

    [Test]
    public void PauseResumeAndClearAreDeterministic()
    {
        var source = new FakeTraceCaptureSource();
        var dispatcher = new ManualUiDispatcher();
        using var viewModel = new TracePageViewModel(source, dispatcher.Post);

        viewModel.IsPaused = true;
        source.Emit(CreateEntry("ignored"));
        Assert.That(dispatcher.PendingCount, Is.Zero);

        viewModel.IsPaused = false;
        source.Emit(CreateEntry("captured"));
        dispatcher.RunAll();
        Assert.That(viewModel.Entries.Select(entry => entry.Message), Is.EqualTo(new[] { "captured" }));

        viewModel.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Entries, Is.Empty);
            Assert.That(viewModel.Status, Does.Contain("0 of 0"));
        });
    }

    [Test]
    public void ClearDiscardsEntriesThatWereQueuedBeforeClear()
    {
        var source = new FakeTraceCaptureSource();
        var dispatcher = new ManualUiDispatcher();
        using var viewModel = new TracePageViewModel(source, dispatcher.Post);

        source.Emit(CreateEntry("stale"));
        viewModel.Clear();
        source.Emit(CreateEntry("current"));
        dispatcher.RunAll();

        Assert.That(viewModel.Entries.Select(entry => entry.Message), Is.EqualTo(new[] { "current" }));
    }

    [Test]
    public void DisposeUnsubscribesSourceAndIgnoresQueuedWork()
    {
        var source = new FakeTraceCaptureSource();
        var dispatcher = new ManualUiDispatcher();
        var viewModel = new TracePageViewModel(source, dispatcher.Post);

        Assert.That(source.SubscriberCount, Is.EqualTo(1));
        source.Emit(CreateEntry("queued"));

        viewModel.Dispose();
        dispatcher.RunAll();
        source.Emit(CreateEntry("after dispose"));

        Assert.Multiple(() =>
        {
            Assert.That(source.IsDisposed, Is.True);
            Assert.That(source.SubscriberCount, Is.Zero);
            Assert.That(viewModel.Entries, Is.Empty);
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void SystemTraceSourceCapturesProcessTraceEvents()
    {
        using var source = new SystemTraceCaptureSource();
        var entries = new List<TraceEntry>();
        source.EntryReceived += entries.Add;

        Trace.TraceWarning("DevScope trace integration test");
        Trace.Flush();

        Assert.That(
            entries,
            Has.Some.Matches<TraceEntry>(entry =>
                entry.Severity == TraceEventType.Warning
                && entry.Message.Contains("DevScope trace integration test", StringComparison.Ordinal)));
    }

    [Test]
    public void AnimationClockCommandsTrackSelectedTargetStateAndDisposeSession()
    {
        var source = new FakeTraceCaptureSource();
        var dispatcher = new ManualUiDispatcher();
        var target = new Border();
        var adapter = new FakeAnimationClockAdapter();
        var viewModel = new TracePageViewModel(
            source,
            dispatcher.Post,
            selectedAnimationTarget: () => target,
            animationClockAdapter: adapter);

        viewModel.AttachAnimationClock();
        viewModel.PauseAnimationClock();
        viewModel.AnimationClockStepMilliseconds = 20m;
        viewModel.StepAnimationClock();

        Assert.Multiple(() =>
        {
            Assert.That(adapter.AttachTarget, Is.SameAs(target));
            Assert.That(viewModel.IsAnimationClockAttached, Is.True);
            Assert.That(viewModel.IsAnimationClockPaused, Is.True);
            Assert.That(viewModel.AnimationClockTarget, Is.EqualTo(nameof(Border)));
            Assert.That(viewModel.AnimationClockState, Is.EqualTo("Paused"));
            Assert.That(viewModel.AnimationClockTime, Is.EqualTo("0.020 s"));
        });

        viewModel.ResetAnimationClock();
        viewModel.ResumeAnimationClock();
        var firstSession = adapter.Session!;
        viewModel.DetachAnimationClock();

        Assert.Multiple(() =>
        {
            Assert.That(firstSession.ResetCount, Is.EqualTo(1));
            Assert.That(firstSession.ResumeCount, Is.EqualTo(1));
            Assert.That(firstSession.DisposeCount, Is.EqualTo(1));
            Assert.That(viewModel.IsAnimationClockAttached, Is.False);
            Assert.That(viewModel.AnimationClockState, Is.EqualTo("Detached"));
            Assert.That(viewModel.AnimationClockTime, Is.EqualTo("0.000 s"));
        });

        viewModel.AttachAnimationClock();
        var secondSession = adapter.Session!;
        viewModel.Dispose();
        Assert.That(secondSession.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void AnimationClockRequiresAnExplicitSelectedAnimatable()
    {
        var source = new FakeTraceCaptureSource();
        var dispatcher = new ManualUiDispatcher();
        var adapter = new FakeAnimationClockAdapter();
        using var viewModel = new TracePageViewModel(
            source,
            dispatcher.Post,
            selectedAnimationTarget: () => null,
            animationClockAdapter: adapter);

        viewModel.AttachAnimationClock();

        Assert.Multiple(() =>
        {
            Assert.That(adapter.AttachTarget, Is.Null);
            Assert.That(viewModel.IsAnimationClockAttached, Is.False);
            Assert.That(viewModel.AnimationClockStatus, Does.Contain("Select an animatable control"));
        });
    }

    [Test]
    public void TraceViewRendersExperimentalClockControls()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var source = new FakeTraceCaptureSource();
            var dispatcher = new ManualUiDispatcher();
            using var viewModel = new TracePageViewModel(
                source,
                dispatcher.Post,
                selectedAnimationTarget: () => new Border(),
                animationClockAdapter: new FakeAnimationClockAdapter());
            var view = new TracePageView(viewModel);
            var window = new Window
            {
                Width = 900,
                Height = 600,
                Content = view,
            };
            window.Show();

            try
            {
                Dispatcher.UIThread.RunJobs();
                var buttonLabels = view.GetVisualDescendants()
                    .OfType<Button>()
                    .Select(button => button.Content as string)
                    .Where(label => label is not null)
                    .ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(buttonLabels, Does.Contain("Attach selected"));
                    Assert.That(buttonLabels, Does.Contain("Pause"));
                    Assert.That(buttonLabels, Does.Contain("Resume"));
                    Assert.That(buttonLabels, Does.Contain("Step"));
                    Assert.That(buttonLabels, Does.Contain("Reset to 0"));
                    Assert.That(buttonLabels, Does.Contain("Detach"));
                    Assert.That(view.GetVisualDescendants().OfType<NumericUpDown>(), Has.Exactly(1).Items);
                });
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static TraceEntry CreateEntry(
        string message,
        TraceEventType severity = TraceEventType.Information,
        string category = "Tests")
    {
        return new TraceEntry(DateTimeOffset.UtcNow, severity, category, message);
    }

    private sealed class FakeTraceCaptureSource : ITraceCaptureSource
    {
        private Action<TraceEntry>? _handlers;

        public int SubscriberCount { get; private set; }

        public bool IsDisposed { get; private set; }

        public event Action<TraceEntry>? EntryReceived
        {
            add
            {
                _handlers += value;
                SubscriberCount++;
            }
            remove
            {
                _handlers -= value;
                SubscriberCount--;
            }
        }

        public void Emit(TraceEntry entry)
        {
            _handlers?.Invoke(entry);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class ManualUiDispatcher
    {
        private readonly Queue<Action> _pending = new();

        public int PendingCount => _pending.Count;

        public void Post(Action action)
        {
            _pending.Enqueue(action);
        }

        public void RunAll()
        {
            while (_pending.TryDequeue(out var action))
            {
                action();
            }
        }
    }

    private sealed class FakeAnimationClockAdapter : IAnimationClockAdapter
    {
        public AnimationClockCompatibility Compatibility { get; } = new(true, "Test clock supported.");

        public Animatable? AttachTarget { get; private set; }

        public FakeAnimationClockSession? Session { get; private set; }

        public bool TryAttach(
            Animatable target,
            out IAnimationClockSession? session,
            out string diagnostic)
        {
            AttachTarget = target;
            Session = new FakeAnimationClockSession(target);
            session = Session;
            diagnostic = "Test clock attached.";
            return true;
        }
    }

    private sealed class FakeAnimationClockSession(Animatable target) : IAnimationClockSession
    {
        public event EventHandler? Changed;

        public Animatable Target { get; } = target;

        public TimeSpan CurrentTime { get; private set; }

        public bool IsPaused { get; private set; }

        public int ResetCount { get; private set; }

        public int ResumeCount { get; private set; }

        public int DisposeCount { get; private set; }

        public void Pause()
        {
            IsPaused = true;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Resume()
        {
            ResumeCount++;
            IsPaused = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Advance(TimeSpan amount)
        {
            CurrentTime += amount;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Reset()
        {
            ResetCount++;
            CurrentTime = TimeSpan.Zero;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
