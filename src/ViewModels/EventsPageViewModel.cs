using System.Collections.ObjectModel;
using Avalonia.Interactivity;
using RolandUI.DevScope.Models;
using RolandUI.DevScope.Shell;

namespace RolandUI.DevScope.ViewModels;

internal class EventsPageViewModel : ReactiveViewModelBase
{
    private readonly static HashSet<RoutedEvent> DefaultEvents =
    [
        Button.ClickEvent,
        InputElement.KeyDownEvent,
        InputElement.KeyUpEvent,
        InputElement.TextInputEvent,
        InputElement.PointerReleasedEvent,
        InputElement.PointerPressedEvent,
    ];

    public EventsPageViewModel(MainViewModel mainViewModel)
    {
        MainView = mainViewModel;

        Nodes = RoutedEventRegistry.Instance.GetAllRegistered()
            .GroupBy(e => e.OwnerType)
            .OrderBy(e => e.Key.Name)
            .Select(g => new EventOwnerTreeNode(g.Key, g, this))
            .ToList();

        EventsFilter = new FilterViewModel();
        EventsFilter.RefreshFilter += HandleEventsFilterRefreshFilter;
        Disposable.Create(() => EventsFilter.RefreshFilter -= HandleEventsFilterRefreshFilter)
            .AddTo(LifetimeDisposables);

        EnableDefault();
    }

    public IReadOnlyList<EventTreeNodeBase> Nodes { get; }

    public ObservableCollection<FiredEvent> RecordedEvents { get; } = new();

    public FiredEvent? SelectedEvent
    {
        get;
        set => SetProperty(ref field, value);
    }

    public EventTreeNodeBase? SelectedNode
    {
        get;
        set => SetProperty(ref field, value);
    }

    public FilterViewModel EventsFilter { get; }

    public MainViewModel MainView { get; }

    public void Clear()
    {
        RecordedEvents.Clear();
    }

    public void DisableAll()
    {
        EvaluateNodeEnabled(_ => false);
    }

    public void EnableDefault()
    {
        EvaluateNodeEnabled(node => DefaultEvents.Contains(node.Event));
    }

    public void RequestTreeNavigateTo(EventChainLink navTarget)
    {
        if (navTarget.Handler is Control control)
        {
            MainView.RequestTreeNavigateTo(control, true);
        }
    }

    public void SelectEventByType(RoutedEvent evt)
    {
        foreach (var node in Nodes)
        {
            var result = FindNode(node, evt);
            if (result is { IsVisible: true })
            {
                SelectedNode = result;
                break;
            }
        }

        static EventTreeNodeBase? FindNode(EventTreeNodeBase node, RoutedEvent eventType)
        {
            if (node is EventTreeNode eventNode && eventNode.Event == eventType) return node;
            return node.Children?.Select(child => FindNode(child, eventType)).OfType<EventTreeNodeBase>().FirstOrDefault();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(disposing);
            return;
        }

        foreach (var node in Nodes)
        {
            node.Dispose();
        }

        base.Dispose(disposing);
    }

    private void EvaluateNodeEnabled(Func<EventTreeNode, bool> eval)
    {
        void ProcessNode(EventTreeNodeBase node)
        {
            if (node is EventTreeNode eventNode)
            {
                node.IsEnabled = eval(eventNode);
            }

            if (node.Children != null)
            {
                foreach (var childNode in node.Children)
                {
                    ProcessNode(childNode);
                }
            }
        }

        foreach (var node in Nodes)
        {
            ProcessNode(node);
        }
    }

    private void UpdateEventFilters()
    {
        foreach (var node in Nodes)
        {
            FilterNode(node, false);
        }

        bool FilterNode(EventTreeNodeBase node, bool isParentVisible)
        {
            var matchesFilter = EventsFilter.Filter(node.Text);
            var hasVisibleChild = false;

            if (node.Children != null)
            {
                foreach (var childNode in node.Children)
                {
                    hasVisibleChild |= FilterNode(childNode, matchesFilter);
                }
            }

            node.IsVisible = hasVisibleChild || matchesFilter || isParentVisible;

            return node.IsVisible;
        }
    }

    private void HandleEventsFilterRefreshFilter(object? sender, EventArgs e)
    {
        UpdateEventFilters();
    }
}
