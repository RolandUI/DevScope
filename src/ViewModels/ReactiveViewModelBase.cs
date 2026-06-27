using Avalonia.Interactivity;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class ReactiveViewModelBase : ViewModelBase, IDisposable
{
    private bool _isDisposed;
    private bool _isLoaded;
    private TopLevel? _topLevel;

    protected CompositeDisposable LifetimeDisposables { get; } = new();

    protected TopLevel TopLevel => _topLevel ?? throw new InvalidOperationException("The view model is not attached to a TopLevel.");

    protected virtual Task ViewLoaded(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task ViewUnloaded()
    {
        return Task.CompletedTask;
    }

    public void Bind(Control target, bool disposeOnUnloaded = false)
    {
        target.DataContext = this;
        CancellationTokenSource? cancellationTokenSource = null;

        // ReSharper disable once AsyncVoidEventHandlerMethod
        async void LoadedHandler(object? sender, RoutedEventArgs args)
        {
            var cancellationToken = CancellationToken.None;
            try
            {
                if (_isDisposed || _isLoaded) return;

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
            catch (Exception e)
            {
                DevToolsDiagnostics.Report(e, "Lifetime Exception: [ViewLoaded]");
            }
        }

        async void UnloadedHandler(object? sender, RoutedEventArgs args)
        {
            try
            {
                if (!_isLoaded) return;

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
                    if (cancellationTokenSource is not null)
                    {
                        cancellationTokenSource.Dispose();
                        cancellationTokenSource = null;
                    }
                }

                if (disposeOnUnloaded)
                {
                    Dispose();
                }
            }
            catch (Exception e)
            {
                DevToolsDiagnostics.Report(e, "Lifetime Exception: [ViewUnloaded]");
            }
        }

        target.Loaded += LoadedHandler;
        target.Unloaded += UnloadedHandler;
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
