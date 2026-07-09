using System.ComponentModel;
using RolandUI.DevScope.Elements.Properties.ViewModels;
using RolandUI.DevScope.Elements.Search;
using RolandUI.DevScope.Elements.Trees;
using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Elements;

internal sealed class ElementsPageViewModel : ReactiveViewModelBase
{
    public ElementsTreeViewModel LogicalTree { get; }

    public ElementsTreeViewModel VisualTree { get; }

    public ElementsTreeViewModel CurrentTree
    {
        get => _currentTree;
        private set
        {
            if (_currentTree == value)
            {
                return;
            }

            var oldTree = _currentTree;
            oldTree.PropertyChanged -= CurrentTreePropertyChanged;
            _currentTree = value;
            _currentTree.PropertyChanged += CurrentTreePropertyChanged;

            _selectionCoordinator.SynchronizeTreeSelection(oldTree, _currentTree);
            Find.AttachTree(_currentTree);
            RaisePropertyChanged();
            UpdateCurrentDetails();
        }
    }

    public ElementDetailsViewModel? CurrentDetails
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public PropertyExplorerViewModel? CurrentPropertyExplorer => CurrentDetails?.PropertyExplorer;

    public ElementsFindViewModel Find { get; }

    public ElementsTreeMode SelectedTreeMode
    {
        get;
        private set => SetProperty(ref field, value);
    }

    private readonly ISelectionCoordinator _selectionCoordinator;
    private ElementsTreeViewModel _currentTree;

    public ElementsPageViewModel(
        ElementsTreeViewModel logicalTree,
        ElementsTreeViewModel visualTree,
        ISelectionCoordinator selectionCoordinator)
    {
        LogicalTree = logicalTree;
        VisualTree = visualTree;
        _selectionCoordinator = selectionCoordinator;
        _currentTree = logicalTree;
        _currentTree.PropertyChanged += CurrentTreePropertyChanged;
        Disposable.Create(() => _currentTree.PropertyChanged -= CurrentTreePropertyChanged).AddTo(LifetimeDisposables);
        Find = new ElementsFindViewModel();
        Find.AttachTree(_currentTree);
    }

    public void SelectLogicalTree()
    {
        SelectTreeMode(ElementsTreeMode.Logical);
    }

    public void SelectVisualTree()
    {
        SelectTreeMode(ElementsTreeMode.Visual);
    }

    public void SelectTreeMode(ElementsTreeMode treeMode)
    {
        SelectedTreeMode = treeMode;
        CurrentTree = treeMode == ElementsTreeMode.Visual ? VisualTree : LogicalTree;
    }

    public ElementsTreeViewModel GetTree(bool isVisualTree)
    {
        return isVisualTree ? VisualTree : LogicalTree;
    }

    private void CurrentTreePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ElementsTreeViewModel.Details))
        {
            UpdateCurrentDetails();
        }
    }

    private void UpdateCurrentDetails()
    {
        CurrentDetails = CurrentTree.Details;
        RaisePropertyChanged(nameof(CurrentPropertyExplorer));
    }
}
