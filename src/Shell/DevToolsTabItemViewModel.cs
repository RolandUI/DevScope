using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Shell;

internal sealed class DevToolsTabItemViewModel(
    MainViewModel owner,
    DevToolsViewKind kind,
    string label,
    string iconKey
) : ViewModelBase
{
    public DevToolsViewKind Kind { get; } = kind;

    public string Label { get; } = label;

    public string IconKey { get; } = iconKey;

    public bool IsSelected => owner.SelectedViewKind == Kind;

    internal void RaiseSelectionChanged()
    {
        RaisePropertyChanged(nameof(IsSelected));
    }
}
