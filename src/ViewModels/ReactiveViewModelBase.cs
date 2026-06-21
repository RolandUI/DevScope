using Avalonia.Interactivity;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class ReactiveViewModelBase : ViewModelBase, IDisposable
{
    private bool _isDisposed;
    private bool _isLoaded;
    private TopLevel? _topLevel;

    protected CompositeDisposable LifetimeDisposables { get; } = new();

    protected TopLevel TopLevel => _topLevel ?? throw new InvalidOperationException("The view model is not attached to a TopLevel.");

    protected internal virtual Task ViewLoaded(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected internal virtual Task ViewUnloaded()
    {
        return Task.CompletedTask;
    }

    public void Bind(Control target, bool disposeOnUnloaded = false)
    {
        target.DataContext = this;
        CancellationTokenSource? cancellationTokenSource = null;

        async void HandleTargetLoaded(object? sender, RoutedEventArgs args)
        {
            var cancellationToken = CancellationToken.None;

            try
            {
                if (_isDisposed || _isLoaded)
                {
                    return;
                }

                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;

                _isLoaded = true;
                _topLevel = TopLevel.GetTopLevel(target);

                await ViewLoaded(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                DevToolsDiagnostics.Report(exception, "Reactive view model failed during ViewLoaded.");
            }
        }

        async void HandleTargetUnloaded(object? sender, RoutedEventArgs args)
        {
            try
            {
                if (!_isLoaded)
                {
                    return;
                }

                _isLoaded = false;

                if (cancellationTokenSource is not null)
                {
                    await cancellationTokenSource.CancelAsync();
                }

                try
                {
                    await ViewUnloaded();
                }
                finally
                {
                    _topLevel = null;
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null;
                }

                if (disposeOnUnloaded)
                {
                    Dispose();
                }
            }
            catch (Exception exception)
            {
                DevToolsDiagnostics.Report(exception, "Reactive view model failed during ViewUnloaded.");
            }
        }

        target.Loaded += HandleTargetLoaded;
        target.Unloaded += HandleTargetUnloaded;

        Disposable.Create(() =>
        {
            target.Loaded -= HandleTargetLoaded;
            target.Unloaded -= HandleTargetUnloaded;
            cancellationTokenSource?.Dispose();
        }).AddTo(LifetimeDisposables);
    }

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
