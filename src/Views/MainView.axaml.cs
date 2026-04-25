using Avalonia.Markup.Xaml;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}