using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using ClassicDiagnostics.Avalonia.Models;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Trees;

internal sealed class TreeNodeViewModel : ViewModelBase, IDisposable
{
    public TreeNodeViewModelCollection Children { get; }

    public string Classes
    {
        get => _classes;
        private set => SetProperty(ref _classes, value);
    }

    public string? ElementName => Model.ElementName;

    public FontWeight FontWeight { get; }

    public bool IsExpanded
    {
        get;
        set => SetProperty(ref field, value);
    }

    public TreeNodeModel Model { get; }

    public TreeNodeViewModel? Parent { get; }

    public string Type => Model.Type;

    private readonly IDisposable? _classesSubscription;
    private bool _isDisposed;
    private string _classes;

    public TreeNodeViewModel(TreeNodeModel model, TreeNodeViewModel? parent = null)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Parent = parent;
        _classes = string.Empty;
        Children = new TreeNodeViewModelCollection(this, model.Children);
        FontWeight = model.IsRoot ? FontWeight.Bold : FontWeight.Normal;

        if (model.Target is StyledElement { Classes: { } classes })
        {
            _classesSubscription = ((IObservable<object?>)classes.GetWeakCollectionChangedObservable())
                .StartWith(null)
                .Subscribe(_ =>
                {
                    Classes = classes.Count > 0 ?
                        $"({string.Join(" ", classes)})" :
                        string.Empty;
                });
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _classesSubscription?.Dispose();
        Children.Dispose();
        Model.Dispose();
        _isDisposed = true;
    }
}

internal sealed class TreeNodeViewModelCollection : IAvaloniaReadOnlyList<TreeNodeViewModel>, IList, IDisposable
{
    private readonly AvaloniaList<TreeNodeViewModel> _inner = [];
    private readonly TreeNodeModelCollection _models;
    private readonly TreeNodeViewModel _owner;
    private bool _isDisposed;

    public TreeNodeViewModelCollection(TreeNodeViewModel owner, TreeNodeModelCollection models)
    {
        _owner = owner;
        _models = models;

        foreach (var model in models)
        {
            _inner.Add(new TreeNodeViewModel(model, owner));
        }

        _models.CollectionChanged += HandleModelsCollectionChanged;
    }

    public TreeNodeViewModel this[int index] => _inner[index];

    public int Count => _inner.Count;

    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => _inner.CollectionChanged += value;
        remove => _inner.CollectionChanged -= value;
    }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => _inner.PropertyChanged += value;
        remove => _inner.PropertyChanged -= value;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _models.CollectionChanged -= HandleModelsCollectionChanged;
        foreach (var node in _inner.ToArray())
        {
            node.Dispose();
        }
        _inner.Clear();
        _isDisposed = true;
    }

    public IEnumerator<TreeNodeViewModel> GetEnumerator()
    {
        return _inner.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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
        return _inner.Contains((TreeNodeViewModel)value!);
    }

    int IList.IndexOf(object? value)
    {
        return _inner.IndexOf((TreeNodeViewModel)value!);
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
        ((ICollection)_inner).CopyTo(array, index);
    }

    private void HandleModelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            DisposeAndClear();
            foreach (var model in _models)
            {
                _inner.Add(new TreeNodeViewModel(model, _owner));
            }
            return;
        }

        if (e.OldItems is not null)
        {
            foreach (var oldModel in e.OldItems.OfType<TreeNodeModel>())
            {
                var oldNode = _inner.FirstOrDefault(node => ReferenceEquals(node.Model, oldModel));
                if (oldNode is not null)
                {
                    _inner.Remove(oldNode);
                    oldNode.Dispose();
                }
            }
        }

        if (e.NewItems is not null)
        {
            var index = e.NewStartingIndex >= 0 ? e.NewStartingIndex : _inner.Count;
            foreach (var newModel in e.NewItems.OfType<TreeNodeModel>())
            {
                _inner.Insert(index++, new TreeNodeViewModel(newModel, _owner));
            }
        }
    }

    private static NotSupportedException CreateReadOnlyException()
    {
        return new NotSupportedException("Tree node view model collections are read-only through IList.");
    }

    private void DisposeAndClear()
    {
        foreach (var node in _inner.ToArray())
        {
            node.Dispose();
        }

        _inner.Clear();
    }
}