namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class ReactiveViewModelBase : ViewModelBase, IDisposable
{
    private bool _isDisposed;

    protected CompositeDisposable LifetimeDisposables { get; } = new();

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Dispose(true);
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        // Subclasses add subscriptions here when the ViewModel owns their lifetime.
        LifetimeDisposables.Dispose();
    }
}
