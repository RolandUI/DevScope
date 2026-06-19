using Avalonia.Interactivity;
using Avalonia.Threading;
using ClassicDiagnostics.Avalonia.Models;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class EventTreeNode(EventOwnerTreeNode parent, RoutedEvent @event, EventsPageViewModel viewModel) : EventTreeNodeBase(parent, @event.Name)
{
    private readonly EventsPageViewModel _parentViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    private IDisposable? _classHandlerRegistration;
    private FiredEvent? _currentEvent;
    private bool _isRegistered;
    private IDisposable? _routeFinishedSubscription;

    public RoutedEvent Event { get; } = @event ?? throw new ArgumentNullException(nameof(@event));

    public override bool? IsEnabled
    {
        get => base.IsEnabled;
        set
        {
            if (base.IsEnabled == value) return;

            base.IsEnabled = value;
            UpdateTracker();

            if (Parent == null || !_updateParent) return;

            try
            {
                Parent._updateChildren = false;
                Parent.UpdateChecked();
            }
            finally
            {
                Parent._updateChildren = true;
            }
        }
    }

    private void UpdateTracker()
    {
        if (!IsEnabled.GetValueOrDefault())
        {
            UnregisterTracker();
            return;
        }

        if (!_isRegistered)
        {
            var allRoutes = RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble;

            // Routed event class handlers are global registrations, so disabling a node must
            // detach the handler rather than only ignoring callbacks at runtime.
            _classHandlerRegistration = Event.AddClassHandler(typeof(object), HandleEvent, allRoutes, true);
            _routeFinishedSubscription = Event.RouteFinished.Subscribe(HandleRouteFinished);

            _isRegistered = true;
        }
    }

    private void UnregisterTracker()
    {
        if (!_isRegistered &&
            _classHandlerRegistration is null &&
            _routeFinishedSubscription is null)
        {
            return;
        }

        _classHandlerRegistration?.Dispose();
        _classHandlerRegistration = null;

        _routeFinishedSubscription?.Dispose();
        _routeFinishedSubscription = null;

        _currentEvent = null;
        _isRegistered = false;
    }

    public override void Dispose()
    {
        UnregisterTracker();
    }

    private void HandleEvent(object? sender, RoutedEventArgs e)
    {
        if (sender is null || !_isRegistered || IsEnabled == false) return;
        if (sender is Visual v && v.DoesBelongToDevTool()) return;

        var handled = e.Handled;
        var route = e.Route;
        var triggerTime = DateTime.Now;

        Dispatcher.UIThread.PostOnDemand(() =>
        {
            if (_currentEvent == null || !_currentEvent.IsPartOfSameEventChain(e))
            {
                _currentEvent = new FiredEvent(e, new EventChainLink(sender, handled, route), triggerTime);
                _parentViewModel.RecordedEvents.Add(_currentEvent);
                while (_parentViewModel.RecordedEvents.Count > 100)
                {
                    _parentViewModel.RecordedEvents.RemoveAt(0);
                }
            }
            else
            {
                _currentEvent.AddToChain(new EventChainLink(sender, handled, route));
            }
        });
    }

    private void HandleRouteFinished(RoutedEventArgs e)
    {
        if (!_isRegistered || IsEnabled == false) return;
        if (e.Source is Visual v && v.DoesBelongToDevTool()) return;

        Dispatcher.UIThread.PostOnDemand(() =>
        {
            if (_currentEvent != null && e.Handled)
            {
                var linkIndex = _currentEvent.EventChain.Count - 1;
                var link = _currentEvent.EventChain[linkIndex];

                link.Handled = true;
                _currentEvent.HandledBy ??= link;
            }
        });
    }
}
