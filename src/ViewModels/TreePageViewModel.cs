using Avalonia.VisualTree;
using ClassicDiagnostics.Avalonia.Models;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class TreePageViewModel : ReactiveViewModelBase
{
    private readonly ISet<string> _pinnedProperties;
    private ControlDetailsViewModel? _details;

    public TreePageViewModel(MainViewModel mainView, TreeNode[] nodes, ISet<string> pinnedProperties)
    {
        MainView = mainView;
        Nodes = nodes;
        _pinnedProperties = pinnedProperties;
        PropertiesFilter = new FilterViewModel();
        PropertiesFilter.RefreshFilter += OnPropertiesFilterRefreshFilter;
        Disposable.Create(() => PropertiesFilter.RefreshFilter -= OnPropertiesFilterRefreshFilter)
            .AddTo(LifetimeDisposables);

        SettersFilter = new FilterViewModel();
        SettersFilter.RefreshFilter += OnSettersFilterRefreshFilter;
        Disposable.Create(() => SettersFilter.RefreshFilter -= OnSettersFilterRefreshFilter)
            .AddTo(LifetimeDisposables);
    }

    public MainViewModel MainView { get; }

    public FilterViewModel PropertiesFilter { get; }

    public FilterViewModel SettersFilter { get; }

    public TreeNode[] Nodes { get; protected set; }

    public TreeNode? SelectedNode
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Details = value != null ?
                    new ControlDetailsViewModel(this, value.Visual, _pinnedProperties) :
                    null;
                Details?.UpdatePropertiesView(MainView.ShowImplementedInterfaces);
                Details?.UpdateStyleFilters();
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

    public event EventHandler<string>? ClipboardCopyRequested;

    public TreeNode? FindNode(Control control)
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
        var node = default(TreeNode);
        var c = control;

        while (node == null && c != null)
        {
            node = FindNode(c);

            if (node == null)
            {
                c = c.GetVisualParent<Control>();
            }
        }

        if (node != null)
        {
            SelectedNode = node;
            ExpandNode(node.Parent);
        }
    }

    public void CopySelector()
    {
        if (SelectedNode?.Visual is Visual currentVisual)
        {
            var selector = GetVisualSelector(currentVisual);
            ClipboardCopyRequested?.Invoke(this, selector);
        }
    }

    public void CopySelectorFromTemplateParent()
    {
        var parts = new List<string>();

        var currentVisual = SelectedNode?.Visual as Visual;
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

            var stack = new Stack<TreeNode>();
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
            var stack = new Stack<TreeNode>();
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
        (SelectedNode?.Visual as Control)?.BringIntoView();
    }

    public void Focus()
    {
        (SelectedNode?.Visual as Control)?.Focus();
    }

    private static string GetVisualSelector(Visual visual)
    {
        var name = string.IsNullOrEmpty(visual.Name) ? "" : $"#{visual.Name}";
        var classes = string.Concat(visual.Classes.Where(c => !c.StartsWith($":")).Select(c => '.' + c));
        var pseudo = string.Concat(visual.Classes.Where(c => c[0] == ':').Select(c => c));
        var type = visual.StyleKey;
        return $$"""{{{type.Assembly.FullName}}}{{type.Namespace}}|{{type.Name}}{{name}}{{classes}}{{pseudo}}""";
    }

    private static void ExpandNode(TreeNode? node)
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

    private static TreeNode? FindNode(TreeNode node, Control control)
    {
        return node.Visual == control ? node : node.Children.Select(child => FindNode(child, control)).OfType<TreeNode>().FirstOrDefault();
    }

    internal void UpdatePropertiesView()
    {
        Details?.UpdatePropertiesView(MainView.ShowImplementedInterfaces);
    }

    private void OnPropertiesFilterRefreshFilter(object? sender, EventArgs e)
    {
        Details?.PropertiesView?.Refresh();
    }

    private void OnSettersFilterRefreshFilter(object? sender, EventArgs e)
    {
        Details?.UpdateStyleFilters();
    }
}
