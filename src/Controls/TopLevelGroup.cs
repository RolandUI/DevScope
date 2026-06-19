using System.Collections.Specialized;

namespace ClassicDiagnostics.Avalonia.Controls;

internal class TopLevelGroup : AvaloniaObject, IDisposable
{
    private readonly NotifyCollectionChangedEventHandler? _collectionChangedHandler;
    private readonly INotifyCollectionChanged? _collectionChangedSource;
    private bool _isDisposed;

    public TopLevelGroup(IDevToolsTopLevelGroup group)
    {
        Group = group;

        if (Group.Items is INotifyCollectionChanged notifyCollectionChanged)
        {
            _collectionChangedSource = notifyCollectionChanged;
            _collectionChangedHandler = OnCollectionChanged;
            notifyCollectionChanged.CollectionChanged += _collectionChangedHandler;
        }
    }

    public IDevToolsTopLevelGroup Group { get; }

    public IReadOnlyList<TopLevel> Items => Group.Items;
    public event EventHandler<TopLevel>? Added;
    public event EventHandler<TopLevel>? Removed;

    public virtual void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        // The application lifetime owns this collection, so a live subscription would keep
        // the transient DevTools root reachable after its window is closed.
        if (_collectionChangedSource is not null && _collectionChangedHandler is not null)
        {
            _collectionChangedSource.CollectionChanged -= _collectionChangedHandler;
        }

        _isDisposed = true;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems is not null)
        {
            foreach (TopLevel oldItem in args.OldItems)
            {
                Removed?.Invoke(this, oldItem);
            }
        }

        if (args.NewItems is not null)
        {
            foreach (TopLevel newItem in args.NewItems)
            {
                Added?.Invoke(this, newItem);
            }
        }
    }
}
