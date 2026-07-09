using System.Collections.Specialized;

namespace RolandUI.DevScope.Rooting;

internal class PresentationRootNode : AvaloniaObject, IDisposable
{
    private readonly NotifyCollectionChangedEventHandler? _collectionChangedHandler;
    private readonly INotifyCollectionChanged? _collectionChangedSource;
    private bool _isDisposed;

    public PresentationRootNode(IDevToolsRootSource source)
    {
        Source = source;

        if (Source.Items is INotifyCollectionChanged notifyCollectionChanged)
        {
            _collectionChangedSource = notifyCollectionChanged;
            _collectionChangedHandler = HandleSourceCollectionChanged;
            notifyCollectionChanged.CollectionChanged += _collectionChangedHandler;
        }
    }

    public IDevToolsRootSource Source { get; }

    public IReadOnlyList<TopLevel> Items => Source.Items;
    public event EventHandler<TopLevel>? Added;
    public event EventHandler<TopLevel>? Removed;

    public virtual void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        // Presentation roots are owned by the host application lifetime. The DevTools host
        // only borrows their collection, so the subscription must die with this synthetic root.
        if (_collectionChangedSource is not null && _collectionChangedHandler is not null)
        {
            _collectionChangedSource.CollectionChanged -= _collectionChangedHandler;
        }

        _isDisposed = true;
    }

    private void HandleSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems is not null)
        {
            foreach (var oldItem in args.OldItems.OfType<TopLevel>())
            {
                Removed?.Invoke(this, oldItem);
            }
        }

        if (args.NewItems is not null)
        {
            foreach (var newItem in args.NewItems.OfType<TopLevel>())
            {
                Added?.Invoke(this, newItem);
            }
        }
    }
}
