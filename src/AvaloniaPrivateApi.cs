using Avalonia.Controls.ApplicationLifetimes;

namespace ClassicDiagnostics.Avalonia;

internal interface IAvaloniaPrivateApiAccessor
{
    IEnumerable<IValueFrameDiagnostic> GetAppliedStyleFrames(StyledElement styledElement);

    TopLevel? GetSingleViewTopLevel(ISingleViewApplicationLifetime lifetime);
}

internal static class AvaloniaPrivateApi
{
    public static IAvaloniaPrivateApiAccessor Current
    {
        get => field ??= AvaloniaPrivateApiAccessor.Instance;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    }
}

internal sealed class AvaloniaPrivateApiAccessor : IAvaloniaPrivateApiAccessor
{
    public static AvaloniaPrivateApiAccessor Instance { get; } = new();

    private AvaloniaPrivateApiAccessor()
    {
    }

    public IEnumerable<IValueFrameDiagnostic> GetAppliedStyleFrames(StyledElement styledElement)
    {
        // Avalonia diagnostics APIs have moved between public, internal, and private
        // surfaces across versions. Keep this as the only boundary for future
        // UnsafeAccessor-first and reflection-fallback implementations.
        return styledElement.GetValueStoreDiagnostic().AppliedFrames;
    }

    public TopLevel? GetSingleViewTopLevel(ISingleViewApplicationLifetime lifetime)
    {
        return lifetime is ISingleTopLevelApplicationLifetime singleTopLevelApplicationLifetime ?
            singleTopLevelApplicationLifetime.TopLevel :
            null;
    }
}