using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal abstract class TreeNode : ViewModelBase, IDisposable
{
    private readonly IDisposable? _classesSubscription;
    private string _classes;

    protected TreeNode(AvaloniaObject avaloniaObject, TreeNode? parent, string? customTypeName = null)
    {
        _classes = string.Empty;
        Parent = parent;
        Type = customTypeName ?? avaloniaObject.GetType().Name;
        Visual = avaloniaObject;
        FontWeight = IsRoot ? FontWeight.Bold : FontWeight.Normal;

        ElementName = (avaloniaObject as INamed)?.Name;

        if (avaloniaObject is StyledElement { Classes: { } classes })
        {
            _classesSubscription = ((IObservable<object?>)classes.GetWeakCollectionChangedObservable())
                .StartWith(null)
                .Subscribe(_ =>
                {
                    if (classes.Count > 0)
                    {
                        Classes = $"({string.Join(" ", classes)})";
                    }
                    else
                    {
                        Classes = string.Empty;
                    }
                });
        }
    }

    private bool IsRoot => Visual is TopLevel or ContextMenu or IPopupHost;

    public FontWeight FontWeight { get; }

    public abstract TreeNodeCollection Children
    {
        get;
    }

    public string Classes
    {
        get => _classes;
        private set => SetProperty(ref _classes, value);
    }

    public string? ElementName
    {
        get;
    }

    public AvaloniaObject Visual
    {
        get;
    }

    public bool IsExpanded
    {
        get;
        set => SetProperty(ref field, value);
    }

    public TreeNode? Parent
    {
        get;
    }

    public string Type
    {
        get;
        private set;
    }

    public void Dispose()
    {
        _classesSubscription?.Dispose();
        Children.Dispose();
    }
}