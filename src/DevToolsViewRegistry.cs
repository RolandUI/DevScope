using ClassicDiagnostics.Avalonia.ViewModels;
using ClassicDiagnostics.Avalonia.Views;

namespace ClassicDiagnostics.Avalonia;

internal sealed class DevToolsViewRegistry
{
    private readonly Dictionary<Type, Func<Control>> _factories = new();

    public static DevToolsViewRegistry Default { get; } = CreateDefault();

    public void Register<TViewModel>(Func<Control> factory)
        where TViewModel : ViewModelBase
    {
        _factories[typeof(TViewModel)] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public Control? Build(object? data)
    {
        if (data is null)
        {
            return null;
        }

        return _factories.TryGetValue(data.GetType(), out var factory) ?
            factory() :
            new TextBlock { Text = $"No view registered for {data.GetType().FullName}" };
    }

    private static DevToolsViewRegistry CreateDefault()
    {
        var registry = new DevToolsViewRegistry();
        registry.Register<TreePageViewModel>(() => new TreePageView());
        registry.Register<ControlDetailsViewModel>(() => new ControlDetailsView());
        registry.Register<EventsPageViewModel>(() => new EventsPageView());
        registry.Register<HotKeyPageViewModel>(() => new HotKeyPageView());
        return registry;
    }
}
