using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Views;

internal partial class TracePageView : ReactiveUserControl<TracePageViewModel>
{
    public TracePageView(TracePageViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}