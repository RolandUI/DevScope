using Avalonia.Controls.Templates;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia;

internal class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        return DevToolsViewRegistry.Default.Build(data);
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
