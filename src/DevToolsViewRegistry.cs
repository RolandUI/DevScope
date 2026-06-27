using Avalonia.Controls.Templates;
using ClassicDiagnostics.Avalonia.Elements;
using ClassicDiagnostics.Avalonia.Elements.Trees;
using ClassicDiagnostics.Avalonia.ViewModels;
using ClassicDiagnostics.Avalonia.Views;
using ClassicDiagnostics.Avalonia.Views.Elements;

namespace ClassicDiagnostics.Avalonia;

internal sealed class DevToolsViewRegistry : IDataTemplate
{
    private readonly Dictionary<Type, Func<object, Control>> _factories = new();

    public DevToolsViewRegistry()
    {
        Register<ElementsPageViewModel>(viewModel => new ElementsPage(viewModel));
        Register<ElementsTreeViewModel>(viewModel => new ElementsTreeView(viewModel));
        Register<EventsPageViewModel>(viewModel => new EventsPageView(viewModel));
        Register<HotKeyPageViewModel>(viewModel => new HotKeyPageView(viewModel));
        Register<TracePageViewModel>(viewModel => new TracePageView(viewModel));
        Register<SettingsPageViewModel>(viewModel => new SettingsPageView(viewModel));

        void Register<TViewModel>(Func<TViewModel, Control> factory) where TViewModel : ViewModelBase
        {
            _factories[typeof(TViewModel)] = data => factory((TViewModel)data);
        }
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

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
