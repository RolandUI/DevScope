using Avalonia.Threading;
using ClassicDiagnostics.Avalonia.Models;
using ClassicDiagnostics.Avalonia.Properties;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal interface ISelectionCoordinator
{
    void Attach(TreePageViewModel logicalTree, TreePageViewModel visualTree);
    void HandleTreeSelectionChanged(TreePageViewModel tree, TreeNodeViewModel? node);
    void SynchronizeTreeSelection(TreePageViewModel oldTree, TreePageViewModel newTree);
    bool SelectControl(Control control, TreePageViewModel? preferredTree);
    void RequestTreeNavigateTo(Control control, bool isVisualTree);
}

internal sealed class SelectionCoordinator(
    IPinnedPropertyStore pinnedProperties,
    Func<bool> showImplementedInterfaces,
    Action<bool> selectTree
) : ISelectionCoordinator
{
    private TreePageViewModel? _logicalTree;
    private TreePageViewModel? _visualTree;

    public void Attach(TreePageViewModel logicalTree, TreePageViewModel visualTree)
    {
        _logicalTree = logicalTree;
        _visualTree = visualTree;
    }

    public void HandleTreeSelectionChanged(TreePageViewModel tree, TreeNodeViewModel? node)
    {
        UpdateDetails(tree, node);
    }

    public void SynchronizeTreeSelection(TreePageViewModel oldTree, TreePageViewModel newTree)
    {
        if (oldTree.SelectedNode?.Model.Target is not Control control)
        {
            newTree.ClearSelectionFromCoordinator();
            return;
        }

        // TreeView cannot select nested items until the destination tab has been laid out.
        // Keep the old delayed behavior, but move the cross-tree decision out of MainViewModel.
        DispatcherTimer.RunOnce(
            () =>
            {
                try
                {
                    if (!SelectControl(control, newTree))
                    {
                        newTree.ClearSelectionFromCoordinator();
                    }
                }
                catch (Exception exception)
                {
                    DevToolsDiagnostics.Report(
                        exception,
                        "Failed to select the previously selected control after switching tree tabs.");
                }
            },
            TimeSpan.FromMilliseconds(0));
    }

    public bool SelectControl(Control control, TreePageViewModel? preferredTree)
    {
        if (preferredTree?.SelectControlFromCoordinator(control) == true)
        {
            UpdateDetails(preferredTree, preferredTree.SelectedNode);
            return true;
        }

        foreach (var tree in new[] { _logicalTree, _visualTree }.Where(tree => tree is not null && tree != preferredTree))
        {
            if (tree!.SelectControlFromCoordinator(control))
            {
                UpdateDetails(tree, tree.SelectedNode);
                return true;
            }
        }

        return false;
    }

    public void RequestTreeNavigateTo(Control control, bool isVisualTree)
    {
        var tree = isVisualTree ? _visualTree : _logicalTree;
        if (tree?.FindNode(control) is null)
        {
            return;
        }

        selectTree(isVisualTree);
        SelectControl(control, tree);
    }

    private void UpdateDetails(TreePageViewModel tree, TreeNodeViewModel? node)
    {
        var details = node is not null ?
            new ControlDetailsViewModel(tree, node.Model.Target, pinnedProperties) :
            null;

        details?.UpdatePropertiesView(showImplementedInterfaces());
        details?.UpdateStyleFilters();
        tree.SetDetails(details);
    }
}
