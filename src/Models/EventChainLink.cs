using Avalonia.Interactivity;

namespace RolandUI.DevScope.Models;

internal class EventChainLink(object handler, bool handled, RoutingStrategies route)
{
    public object Handler { get; } = handler ?? throw new ArgumentNullException(nameof(handler));

    public bool BeginsNewRoute { get; set; }

    public string HandlerName
    {
        get
        {
            if (Handler is INamed named && !string.IsNullOrEmpty(named.Name))
            {
                return named.Name + " (" + Handler.GetType().Name + ")";
            }

            return Handler.GetType().Name;
        }
    }

    public bool Handled { get; set; } = handled;

    public RoutingStrategies Route { get; } = route;
}