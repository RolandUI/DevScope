using Avalonia;
using RolandUI.DevScope.Shell;

namespace RolandUI.DevScope.Views.Shell;

internal partial class EmbeddedDevToolsView : ReactiveUserControl<MainViewModel>, IDevToolsVisual
{
    public EmbeddedDevToolsView(MainViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredSize = base.MeasureOverride(availableSize);
        return new Size(
            double.IsInfinity(availableSize.Width) ? desiredSize.Width : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? desiredSize.Height : availableSize.Height);
    }
}
