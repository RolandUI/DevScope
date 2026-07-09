namespace RolandUI.DevScope.Extensions;

internal static class DisposableExtensions
{
    /// <summary>
    /// Registers a disposable with its owner collection at the call site that creates it.
    /// </summary>
    /// <remarks>
    /// This intentionally stays tiny and dependency-free. DevTools has many global Avalonia
    /// subscriptions, and keeping creation next to ownership makes leak reviews much easier.
    /// </remarks>
    public static void AddTo(this IDisposable disposable, ICollection<IDisposable> disposables)
    {
        disposables.Add(disposable);
    }
}
