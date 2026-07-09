using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Views;

internal abstract class ReactiveUserControl<TViewModel> : UserControl where TViewModel : ViewModelBase
{
    public TViewModel? ViewModel
    {
        get;
        private set;
    }

    protected TViewModel RequiredViewModel =>
        ViewModel ?? throw new InvalidOperationException($"View model '{typeof(TViewModel).GetDetailedTypeName()}' is not attached.");

    protected ReactiveUserControl()
    {
    }

    protected ReactiveUserControl(TViewModel viewModel, bool disposeOnUnloaded = false)
    {
        AttachViewModel(viewModel, disposeOnUnloaded);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        ViewModel = DataContext as TViewModel;
    }

    private void AttachViewModel(TViewModel viewModel, bool disposeOnUnloaded)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        if (viewModel is ReactiveViewModelBase reactiveViewModel)
        {
            reactiveViewModel.Bind(this, disposeOnUnloaded);
        }
        else
        {
            DataContext = viewModel;
        }
    }
}
