using Avalonia.Markup.Xaml;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class ControlStylesView : ReactiveUserControl<ControlDetailsViewModel>
{
    public ControlStylesView()
    {
        InitializeComponent();
    }

    public ControlStylesView(ControlDetailsViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        LoadComponent();
    }

    public void HandlePropertyNamePointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: SetterViewModel setterViewModel })
        {
            RequiredViewModel.SelectProperty(setterViewModel.Property);
        }
    }
}