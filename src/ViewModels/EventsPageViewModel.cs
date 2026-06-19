using System.Collections.ObjectModel;
using Avalonia.Interactivity;
using ClassicDiagnostics.Avalonia.Models;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class EventsPageViewModel : ViewModelBase, IDisposable
{
    private readonly static HashSet<RoutedEvent> s_defaultEvents = new()
    {
        Button.ClickEvent,
        InputElement.KeyDownEvent,
        InputElement.KeyUpEvent,
        InputElement.TextInputEvent,
        InputElement.PointerReleasedEvent,
        InputElement.PointerPressedEvent,
    };

    public EventsPageViewModel(MainViewModel mainViewModel)
    {
        MainView = mainViewModel;

        Nodes = RoutedEventRegistry.Instance.GetAllRegistered()
            .GroupBy(e => e.OwnerType)
            .OrderBy(e => e.Key.Name)
            .Select(g => new EventOwnerTreeNode(g.Key, g, this))
            .ToArray();

        EventsFilter = new FilterViewModel();
        EventsFilter.RefreshFilter += OnEventsFilterRefreshFilter;

        EnableDefault();
    }

    public string Name => "Events";

    public EventTreeNodeBase[] Nodes { get; }

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
        EvaluateNodeEnabled(node => s_defaultEvents.Contains(node.Event));
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

            if (result != null && result.IsVisible)
            {
                SelectedNode = result;

                break;
            }
        }

        static EventTreeNodeBase? FindNode(EventTreeNodeBase node, RoutedEvent eventType)
        {
            if (node is EventTreeNode eventNode && eventNode.Event == eventType)
            {
                return node;
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var result = FindNode(child, eventType);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }

    public void Dispose()
    {
        EventsFilter.RefreshFilter -= OnEventsFilterRefreshFilter;

        foreach (var node in Nodes)
        {
            node.Dispose();
        }
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

    private void OnEventsFilterRefreshFilter(object? sender, EventArgs e)
    {
        UpdateEventFilters();
    }
}
