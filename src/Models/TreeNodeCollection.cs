using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Collections;

namespace ClassicDiagnostics.Avalonia.Models;

internal abstract class TreeNodeCollection(TreeNode owner) : IAvaloniaReadOnlyList<TreeNode>, IList, IDisposable
{
    internal readonly static TreeNodeCollection Empty = new EmptyTreeNodeCollection();

    private AvaloniaList<TreeNode>? _inner;

    protected TreeNode Owner { get; } = owner;

    public TreeNode this[int index] => EnsureInitialized()[index];

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

    public IEnumerator<TreeNode> GetEnumerator()
    {
        return EnsureInitialized().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public virtual void Dispose()
    {
        if (_inner == null) return;

        foreach (var node in _inner)
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
        return EnsureInitialized().Contains((TreeNode)value!);
    }

    int IList.IndexOf(object? value)
    {
        return EnsureInitialized().IndexOf((TreeNode)value!);
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

    protected abstract void Initialize(AvaloniaList<TreeNode> nodes);

    private static NotSupportedException CreateReadOnlyException()
    {
        return new NotSupportedException("Tree node collections are read-only through IList.");
    }

    private AvaloniaList<TreeNode> EnsureInitialized()
    {
        if (_inner is null)
        {
            _inner = new AvaloniaList<TreeNode>();
            Initialize(_inner);
        }
        return _inner;
    }

    private class EmptyTreeNodeCollection() : TreeNodeCollection(null!) // TODO: check null! safety
    {
        protected override void Initialize(AvaloniaList<TreeNode> nodes)
        {

        }
    }
}
