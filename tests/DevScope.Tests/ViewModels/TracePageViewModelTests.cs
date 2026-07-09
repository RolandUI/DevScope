using System.Diagnostics;
using RolandUI.DevScope.Tracing;
using RolandUI.DevScope.ViewModels;

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
}
