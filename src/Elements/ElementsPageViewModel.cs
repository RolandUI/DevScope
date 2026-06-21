using System.ComponentModel;
using ClassicDiagnostics.Avalonia.Elements.Search;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements;

internal sealed class ElementsPageViewModel : ReactiveViewModelBase
{
    public TreePageViewModel LogicalTree { get; }

    public TreePageViewModel VisualTree { get; }

    public TreePageViewModel CurrentTree
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

    public ControlDetailsViewModel? CurrentDetails
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public ElementsFindViewModel Find { get; }

    public ElementsTreeMode SelectedTreeMode
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RaisePropertyChanged(nameof(IsLogicalTreeSelected));
                RaisePropertyChanged(nameof(IsVisualTreeSelected));
            }
        }
    }

    public bool IsLogicalTreeSelected => SelectedTreeMode == ElementsTreeMode.Logical;

    public bool IsVisualTreeSelected => SelectedTreeMode == ElementsTreeMode.Visual;

    private readonly ISelectionCoordinator _selectionCoordinator;
    private TreePageViewModel _currentTree;

    public ElementsPageViewModel(
        TreePageViewModel logicalTree,
        TreePageViewModel visualTree,
        ISelectionCoordinator selectionCoordinator)
    {
        LogicalTree = logicalTree;
        VisualTree = visualTree;
        _selectionCoordinator = selectionCoordinator;
        _currentTree = logicalTree;
        _currentTree.PropertyChanged += CurrentTreePropertyChanged;
        Disposable.Create(() => _currentTree.PropertyChanged -= CurrentTreePropertyChanged).AddTo(LifetimeDisposables);
        Find = new ElementsFindViewModel(new TreeSearchService());
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

    public TreePageViewModel GetTree(bool isVisualTree)
    {
        return isVisualTree ? VisualTree : LogicalTree;
    }

    private void CurrentTreePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TreePageViewModel.Details))
        {
            UpdateCurrentDetails();
        }
    }

    private void UpdateCurrentDetails()
    {
        CurrentDetails = CurrentTree.Details;
    }
}