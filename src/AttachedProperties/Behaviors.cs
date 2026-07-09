using Avalonia.Interactivity;

namespace RolandUI.DevScope.AttachedProperties;

public static class Behaviors
{
    public static readonly AttachedProperty<bool> SwallowBringIntoViewEventProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, bool>("SwallowBringIntoViewEvent");

    public static void SetSwallowBringIntoViewEvent(Control obj, bool value) => obj.SetValue(SwallowBringIntoViewEventProperty, value);

    public static bool GetSwallowBringIntoViewEvent(Control obj) => obj.GetValue(SwallowBringIntoViewEventProperty);

    private static readonly Dictionary<Control, IDisposable> Handlers = new();

    static Behaviors()
    {
        SwallowBringIntoViewEventProperty.Changed.AddClassHandler<Control>(HandleSwallowBringIntoViewEventChanged);
    }

    private static void HandleSwallowBringIntoViewEventChanged(Control sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is true)
        {
            if (Handlers.TryGetValue(sender, out var existingHandler))
            {
                existingHandler.Dispose();
            }

            Handlers[sender] = sender.AddDisposableHandler(
                Control.RequestBringIntoViewEvent,
                (_, e) => e.Handled = true,
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble | RoutingStrategies.Direct);
        }
        else
        {
            if (Handlers.TryGetValue(sender, out var existingHandler))
            {
                existingHandler.Dispose();
                Handlers.Remove(sender);
            }
        }
    }
}