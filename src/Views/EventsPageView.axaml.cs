using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Models;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class EventsPageView : UserControl
{
    private readonly ListBox _events;
    private IDisposable? _adorner;
    private EventsPageViewModel? _recordedEventsOwner;

    public EventsPageView()
    {
        InitializeComponent();
        _events = this.GetControl<ListBox>("EventsList");
    }

    public void NavigateTo(object sender, TappedEventArgs e)
    {
        if (DataContext is not EventsPageViewModel viewModel || sender is not Control control) return;

        switch (control.Tag)
        {
            case EventChainLink chainLink:
            {
                viewModel.RequestTreeNavigateTo(chainLink);
                break;
            }
            case RoutedEvent evt:
            {
                viewModel.SelectEventByType(evt);

                break;
            }
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_recordedEventsOwner is not null)
        {
            _recordedEventsOwner.RecordedEvents.CollectionChanged -= OnRecordedEventsChanged;
            _recordedEventsOwner = null;
        }

        if (DataContext is EventsPageViewModel viewModel)
        {
            viewModel.RecordedEvents.CollectionChanged += OnRecordedEventsChanged;
            _recordedEventsOwner = viewModel;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_recordedEventsOwner is not null)
        {
            _recordedEventsOwner.RecordedEvents.CollectionChanged -= OnRecordedEventsChanged;
            _recordedEventsOwner = null;
        }

        _adorner?.Dispose();
        _adorner = null;
    }

    private void OnRecordedEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not ObservableCollection<FiredEvent> events) return;

        var @event = events.LastOrDefault();
        if (@event is null) return;

        Dispatcher.UIThread.Post(() => _events.ScrollIntoView(@event));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ListBoxItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (DataContext is EventsPageViewModel viewModel && sender is Control { DataContext: EventChainLink { Handler: Visual visual } })
        {
            _adorner = ControlHighlightAdorner.Add(visual, viewModel.MainView.ShouldVisualizeMarginPadding);
        }
    }

    private void ListBoxItem_PointerExited(object? sender, PointerEventArgs e)
    {
        _adorner?.Dispose();
        _adorner = null;
    }
}
