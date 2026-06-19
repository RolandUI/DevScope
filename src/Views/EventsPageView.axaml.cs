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
        if (DataContext is EventsPageViewModel vm && sender is Control control)
        {
            switch (control.Tag)
            {
                case EventChainLink chainLink:
                {
                    vm.RequestTreeNavigateTo(chainLink);
                    break;
                }
                case RoutedEvent evt:
                {
                    vm.SelectEventByType(evt);

                    break;
                }
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

        if (DataContext is EventsPageViewModel vm)
        {
            vm.RecordedEvents.CollectionChanged += OnRecordedEventsChanged;
            _recordedEventsOwner = vm;
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
        if (sender is ObservableCollection<FiredEvent> events)
        {
            var evt = events.LastOrDefault();

            if (evt is null)
            {
                return;
            }

            Dispatcher.UIThread.Post(() => _events.ScrollIntoView(evt));
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ListBoxItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (DataContext is EventsPageViewModel vm
            && sender is Control control
            && control.DataContext is EventChainLink chainLink
            && chainLink.Handler is Visual visual)
        {
            _adorner = ControlHighlightAdorner.Add(visual, vm.MainView.ShouldVisualizeMarginPadding);
        }
    }

    private void ListBoxItem_PointerExited(object? sender, PointerEventArgs e)
    {
        _adorner?.Dispose();
        _adorner = null;
    }
}
