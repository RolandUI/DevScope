using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Models;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class EventsPageView : ReactiveUserControl<EventsPageViewModel>
{
    private IDisposable? _adorner;
    private EventsPageViewModel? _recordedEventsOwner;

    public EventsPageView()
    {
        InitializeComponent();
    }

    public EventsPageView(EventsPageViewModel viewModel)
        : base(viewModel)
    {
        InitializeComponent();
    }

    public void HandleNavigateDoubleTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        var viewModel = RequiredViewModel;

        switch (control.Tag)
        {
            case EventChainLink chainLink:
            {
                viewModel.RequestTreeNavigateTo(chainLink);
                break;
            }
            case RoutedEvent routedEvent:
            {
                viewModel.SelectEventByType(routedEvent);

                break;
            }
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_recordedEventsOwner is not null)
        {
            _recordedEventsOwner.RecordedEvents.CollectionChanged -= HandleRecordedEventsChanged;
            _recordedEventsOwner = null;
        }

        if (ViewModel is { } viewModel)
        {
            viewModel.RecordedEvents.CollectionChanged += HandleRecordedEventsChanged;
            _recordedEventsOwner = viewModel;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_recordedEventsOwner is not null)
        {
            _recordedEventsOwner.RecordedEvents.CollectionChanged -= HandleRecordedEventsChanged;
            _recordedEventsOwner = null;
        }

        _adorner?.Dispose();
        _adorner = null;
    }

    private void HandleRecordedEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not ObservableCollection<FiredEvent> events) return;

        var @event = events.LastOrDefault();
        if (@event is null) return;

        Dispatcher.UIThread.Post(() => EventsList.ScrollIntoView(@event));
    }

    private void InitializeComponent()
    {
        LoadComponent();
    }

    private void HandleEventChainItemPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: EventChainLink { Handler: Visual visual } })
        {
            _adorner = ControlHighlightAdorner.Add(visual, RequiredViewModel.MainView.ShouldVisualizeMarginPadding);
        }
    }

    private void HandleEventChainItemPointerExited(object? sender, PointerEventArgs e)
    {
        _adorner?.Dispose();
        _adorner = null;
    }
}
