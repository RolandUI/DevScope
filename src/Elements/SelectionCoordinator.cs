using Avalonia.Threading;
using RolandUI.DevScope.Elements.Properties;
using RolandUI.DevScope.Elements.Trees;

namespace RolandUI.DevScope.Elements;

internal interface ISelectionCoordinator
{
    void Attach(ElementsTreeViewModel logicalTree, ElementsTreeViewModel visualTree);
    void HandleTreeSelectionChanged(ElementsTreeViewModel tree, TreeNodeViewModel? node);
    void SynchronizeTreeSelection(ElementsTreeViewModel oldTree, ElementsTreeViewModel newTree);
    bool SelectControl(Control control, ElementsTreeViewModel? preferredTree);
    void RequestTreeNavigateTo(Control control, bool isVisualTree);
}

internal sealed class SelectionCoordinator(
    IPinnedPropertyStore pinnedProperties,
    Func<bool> showImplementedInterfaces,
    Action<bool> selectTree
) : ISelectionCoordinator
{
    private ElementsTreeViewModel? _logicalTree;
    private ElementsTreeViewModel? _visualTree;

    public void Attach(ElementsTreeViewModel logicalTree, ElementsTreeViewModel visualTree)
    {
        _logicalTree = logicalTree;
        _visualTree = visualTree;
    }

    public void HandleTreeSelectionChanged(ElementsTreeViewModel tree, TreeNodeViewModel? node)
    {
        UpdateDetails(tree, node);
    }

    public void SynchronizeTreeSelection(ElementsTreeViewModel oldTree, ElementsTreeViewModel newTree)
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

    public bool SelectControl(Control control, ElementsTreeViewModel? preferredTree)
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

    private void UpdateDetails(ElementsTreeViewModel tree, TreeNodeViewModel? node)
    {
        var details = node is not null ?
            new ElementDetailsViewModel(tree, node.Model.Target, pinnedProperties) :
            null;

        details?.UpdatePropertiesView(showImplementedInterfaces());
        details?.UpdateStyleFilters();
        tree.SetDetails(details);
    }
}