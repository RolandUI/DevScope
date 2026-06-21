using Avalonia.Interactivity;
using Avalonia.Threading;
using ClassicDiagnostics.Avalonia.Views;

namespace ClassicDiagnostics.Avalonia.Elements;

internal partial class ElementsPageView : ReactiveUserControl<ElementsPageViewModel>
{
    public ElementsPageView(ElementsPageViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, HandleTargetKeyDown, RoutingStrategies.Tunnel);
    }

    private void InitializeComponent()
    {
        LoadComponent();
    }

    private void HandleCloseFindClick(object? sender, RoutedEventArgs e)
    {
        RequiredViewModel.Find.Close();
    }

    private void HandleFindNextClick(object? sender, RoutedEventArgs e)
    {
        RequiredViewModel.Find.FindNext();
    }

    private void HandleFindPreviousClick(object? sender, RoutedEventArgs e)
    {
        RequiredViewModel.Find.FindPrevious();
    }

    private void FocusFindTextBox()
    {
        Dispatcher.UIThread.Post(() =>
        {
            FindTextBox.Focus();
            FindTextBox.SelectAll();
        });
    }

    private void HandleTargetKeyDown(object? sender, KeyEventArgs e)
    {
        var viewModel = RequiredViewModel;

        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            viewModel.Find.Show();
            FocusFindTextBox();
            e.Handled = true;
            return;
        }

        if (!viewModel.Find.IsVisible)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                viewModel.Find.Close();
                e.Handled = true;
                break;
            case Key.Enter when e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                viewModel.Find.FindPrevious();
                e.Handled = true;
                break;
            case Key.Enter:
                viewModel.Find.FindNext();
                e.Handled = true;
                break;
        }
    }
}
