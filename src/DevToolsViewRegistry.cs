using ClassicDiagnostics.Avalonia.ViewModels;
using ClassicDiagnostics.Avalonia.Views;
using ClassicDiagnostics.Avalonia.Elements;

namespace ClassicDiagnostics.Avalonia;

internal sealed class DevToolsViewRegistry
{
    private readonly Dictionary<Type, Func<object, Control>> _factories = new();

    public static DevToolsViewRegistry Default { get; } = CreateDefault();

    public void Register<TViewModel>(Func<TViewModel, Control> factory)
        where TViewModel : ViewModelBase
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factories[typeof(TViewModel)] = data => factory((TViewModel)data);
    }

    public Control? Build(object? data)
    {
        if (data is null)
        {
            return null;
        }

        return _factories.TryGetValue(data.GetType(), out var factory) ?
            factory(data) :
            new TextBlock { Text = $"No view registered for {data.GetType().FullName}" };
    }

    private static DevToolsViewRegistry CreateDefault()
    {
        var registry = new DevToolsViewRegistry();
        registry.Register<ElementsPageViewModel>(viewModel => new ElementsPageView(viewModel));
        registry.Register<TreePageViewModel>(viewModel => new TreePageView(viewModel));
        registry.Register<ControlDetailsViewModel>(viewModel => new ControlDetailsView(viewModel));
        registry.Register<EventsPageViewModel>(viewModel => new EventsPageView(viewModel));
        registry.Register<HotKeyPageViewModel>(viewModel => new HotKeyPageView(viewModel));
        registry.Register<TracePageViewModel>(viewModel => new TracePageView(viewModel));
        registry.Register<SettingsPageViewModel>(viewModel => new SettingsPageView(viewModel));
        return registry;
    }
}
