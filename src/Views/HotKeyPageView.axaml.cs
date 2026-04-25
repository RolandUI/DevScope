using Avalonia.Markup.Xaml;

namespace ClassicDiagnostics.Avalonia.Views
{
    internal partial class HotKeyPageView : UserControl
    {
        public HotKeyPageView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
