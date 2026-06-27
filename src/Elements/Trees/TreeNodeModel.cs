using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using ClassicDiagnostics.Avalonia.Rooting;

namespace ClassicDiagnostics.Avalonia.Elements.Trees;

internal sealed class TreeNodeModel : IDisposable
{
    public TreeNodeModelCollection Children { get; }

    public string? ElementName { get; }

    public bool IsRoot { get; }

    public TreeNodeModel? Parent { get; }

    public AvaloniaObject Target { get; }

    public string Type { get; }

    private bool _isDisposed;

    public TreeNodeModel(
        AvaloniaObject target,
        TreeNodeModel? parent,
        Func<TreeNodeModel, TreeNodeModelCollection> childrenFactory,
        string? customTypeName = null)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Parent = parent;
        Type = customTypeName ?? GetTypeName(target);
        ElementName = (target as INamed)?.Name;
        IsRoot = parent is null || target is TopLevel or ContextMenu or IPopupHost;
        Children = childrenFactory(this);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Children.Dispose();
        _isDisposed = true;
    }

    private static string GetTypeName(AvaloniaObject target)
    {
        return target is ApplicationRootNode applicationPage ?
            applicationPage.DiagnosticTypeName :
            target.GetType().Name;
    }
}

internal abstract class TreeNodeModelCollection(TreeNodeModel? owner) : IAvaloniaReadOnlyList<TreeNodeModel>, IList, IDisposable
{
    private AvaloniaList<TreeNodeModel>? _inner;

    internal static TreeNodeModelCollection Empty { get; } = new EmptyTreeNodeModelCollection();

    protected TreeNodeModel? Owner { get; } = owner;

    public TreeNodeModel this[int index] => EnsureInitialized()[index];

    public int Count => EnsureInitialized().Count;

    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => EnsureInitialized().CollectionChanged += value;
        remove => EnsureInitialized().CollectionChanged -= value;
    }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => EnsureInitialized().PropertyChanged += value;
        remove => EnsureInitialized().PropertyChanged -= value;
    }

    public IEnumerator<TreeNodeModel> GetEnumerator()
    {
        return EnsureInitialized().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public virtual void Dispose()
    {
        if (_inner is null)
        {
            return;
        }

        foreach (var node in _inner.ToArray())
        {
            node.Dispose();
        }
    }

    bool IList.IsFixedSize => false;
    bool IList.IsReadOnly => true;
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;

    object? IList.this[int index]
    {
        get => this[index];
        set => throw CreateReadOnlyException();
    }

    int IList.Add(object? value)
    {
        throw CreateReadOnlyException();
    }

    void IList.Clear()
    {
        throw CreateReadOnlyException();
    }

    bool IList.Contains(object? value)
    {
        return EnsureInitialized().Contains((TreeNodeModel)value!);
    }

    int IList.IndexOf(object? value)
    {
        return EnsureInitialized().IndexOf((TreeNodeModel)value!);
    }

    void IList.Insert(int index, object? value)
    {
        throw CreateReadOnlyException();
    }

    void IList.Remove(object? value)
    {
        throw CreateReadOnlyException();
    }

    void IList.RemoveAt(int index)
    {
        throw CreateReadOnlyException();
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ((ICollection)EnsureInitialized()).CopyTo(array, index);
    }

    protected abstract void Initialize(AvaloniaList<TreeNodeModel> nodes);

    protected static void DisposeAndClear(AvaloniaList<TreeNodeModel> nodes)
    {
        foreach (var node in nodes.ToArray())
        {
            node.Dispose();
        }

        nodes.Clear();
    }

    protected static void RemoveAndDisposeAt(AvaloniaList<TreeNodeModel> nodes, int index)
    {
        var node = nodes[index];
        nodes.RemoveAt(index);
        node.Dispose();
    }

    protected static void RemoveAndDispose(AvaloniaList<TreeNodeModel> nodes, TreeNodeModel node)
    {
        if (nodes.Remove(node))
        {
            node.Dispose();
        }
    }

    private static NotSupportedException CreateReadOnlyException()
    {
        return new NotSupportedException("Tree node model collections are read-only through IList.");
    }

    private AvaloniaList<TreeNodeModel> EnsureInitialized()
    {
        if (_inner is null)
        {
            _inner = [];
            Initialize(_inner);
        }

        return _inner;
    }

    private sealed class EmptyTreeNodeModelCollection() : TreeNodeModelCollection(null)
    {
        protected override void Initialize(AvaloniaList<TreeNodeModel> nodes)
        {
        }
    }
}