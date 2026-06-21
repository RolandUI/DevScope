using Avalonia.VisualTree;
using ClassicDiagnostics.Avalonia.Models;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class TreePageViewModel : ReactiveViewModelBase
{
    private readonly ISelectionCoordinator _selectionCoordinator;
    private ControlDetailsViewModel? _details;
    private TreeNodeViewModel? _selectedNode;

    public TreePageViewModel(
        MainViewModel mainView,
        IReadOnlyList<TreeNodeModel> nodes,
        ISelectionCoordinator selectionCoordinator)
    {
        MainView = mainView;
        _selectionCoordinator = selectionCoordinator;
        Nodes = nodes.Select(node => new TreeNodeViewModel(node)).ToArray();

        PropertiesFilter = new FilterViewModel();
        PropertiesFilter.RefreshFilter += HandlePropertiesFilterRefreshFilter;
        Disposable.Create(() => PropertiesFilter.RefreshFilter -= HandlePropertiesFilterRefreshFilter)
            .AddTo(LifetimeDisposables);

        SettersFilter = new FilterViewModel();
        SettersFilter.RefreshFilter += HandleSettersFilterRefreshFilter;
        Disposable.Create(() => SettersFilter.RefreshFilter -= HandleSettersFilterRefreshFilter)
            .AddTo(LifetimeDisposables);
    }

    public MainViewModel MainView { get; }

    public FilterViewModel PropertiesFilter { get; }

    public FilterViewModel SettersFilter { get; }

    public IReadOnlyList<TreeNodeViewModel> Nodes { get; }

    public TreeNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                _selectionCoordinator.HandleTreeSelectionChanged(this, value);
            }
        }
    }

    public ControlDetailsViewModel? Details
    {
        get => _details;
        private set
        {
            var oldValue = _details;

            if (SetProperty(ref _details, value))
            {
                oldValue?.Dispose();
            }
        }
    }

    public event EventHandler<string>? ClipboardCopyRequested;

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(disposing);
            return;
        }

        foreach (var node in Nodes)
        {
            node.Dispose();
        }

        _details?.Dispose();
        base.Dispose(disposing);
    }

    public TreeNodeViewModel? FindNode(Control control)
    {
        foreach (var node in Nodes)
        {
            var result = FindNode(node, control);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public void SelectControl(Control control)
    {
        _selectionCoordinator.SelectControl(control, this);
    }

    public void CopySelector()
    {
        if (SelectedNode?.Model.Target is Visual currentVisual)
        {
            var selector = GetVisualSelector(currentVisual);
            ClipboardCopyRequested?.Invoke(this, selector);
        }
    }

    public void CopySelectorFromTemplateParent()
    {
        var parts = new List<string>();

        var currentVisual = SelectedNode?.Model.Target as Visual;
        while (currentVisual is not null)
        {
            parts.Add(GetVisualSelector(currentVisual));

            currentVisual = currentVisual.TemplatedParent as Visual;
        }

        if (parts.Count != 0)
        {
            parts.Reverse();
            var selector = string.Join(" /template/ ", parts);
            ClipboardCopyRequested?.Invoke(this, selector);
        }
    }

    public void ExpandRecursively()
    {
        if (SelectedNode is { } selectedNode)
        {
            ExpandNode(selectedNode);

            var stack = new Stack<TreeNodeViewModel>();
            stack.Push(selectedNode);

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                item.IsExpanded = true;
                foreach (var child in item.Children)
                {
                    stack.Push(child);
                }
            }
        }
    }

    public void CollapseChildren()
    {
        if (SelectedNode is { } selectedNode)
        {
            var stack = new Stack<TreeNodeViewModel>();
            stack.Push(selectedNode);

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                item.IsExpanded = false;
                foreach (var child in item.Children)
                {
                    stack.Push(child);
                }
            }
        }
    }

    public void CaptureNodeScreenshot()
    {
        MainView.Shot(null);
    }

    public void BringIntoView()
    {
        (SelectedNode?.Model.Target as Control)?.BringIntoView();
    }

    public void Focus()
    {
        (SelectedNode?.Model.Target as Control)?.Focus();
    }

    internal bool SelectControlFromCoordinator(Control control)
    {
        var node = default(TreeNodeViewModel);
        var current = control;

        while (node == null && current != null)
        {
            node = FindNode(current);

            if (node == null)
            {
                current = current.GetVisualParent<Control>();
            }
        }

        if (node == null)
        {
            return false;
        }

        SetSelectedNodeFromCoordinator(node);
        ExpandNode(node.Parent);
        return true;
    }

    internal void SetDetails(ControlDetailsViewModel? details)
    {
        Details = details;
    }

    internal void SelectAndRevealNode(TreeNodeViewModel node)
    {
        SelectedNode = node;
        ExpandNode(node.Parent);
    }

    internal void ClearSelectionFromCoordinator()
    {
        SetSelectedNodeFromCoordinator(null);
        SetDetails(null);
    }

    internal void UpdatePropertiesView()
    {
        Details?.UpdatePropertiesView(MainView.ShowImplementedInterfaces);
    }

    private void SetSelectedNodeFromCoordinator(TreeNodeViewModel? node)
    {
        SetProperty(ref _selectedNode, node, nameof(SelectedNode));
    }

    private static string GetVisualSelector(Visual visual)
    {
        var name = string.IsNullOrEmpty(visual.Name) ? "" : $"#{visual.Name}";
        var classes = string.Concat(visual.Classes.Where(c => !c.StartsWith($":")).Select(c => '.' + c));
        var pseudo = string.Concat(visual.Classes.Where(c => c[0] == ':').Select(c => c));
        var type = visual.StyleKey;
        return $$"""{{{type.Assembly.FullName}}}{{type.Namespace}}|{{type.Name}}{{name}}{{classes}}{{pseudo}}""";
    }

    private static void ExpandNode(TreeNodeViewModel? node)
    {
        while (true)
        {
            if (node != null)
            {
                node.IsExpanded = true;
                node = node.Parent;
                continue;
            }

            break;
        }
    }

    private static TreeNodeViewModel? FindNode(TreeNodeViewModel node, Control control)
    {
        return node.Model.Target == control ?
            node :
            node.Children.Select(child => FindNode(child, control)).OfType<TreeNodeViewModel>().FirstOrDefault();
    }

    private void HandlePropertiesFilterRefreshFilter(object? sender, EventArgs e)
    {
        Details?.PropertiesView?.Refresh();
    }

    private void HandleSettersFilterRefreshFilter(object? sender, EventArgs e)
    {
        Details?.UpdateStyleFilters();
    }
}
