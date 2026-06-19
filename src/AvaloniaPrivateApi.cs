namespace ClassicDiagnostics.Avalonia;

internal interface IAvaloniaPrivateApiAccessor
{
    IEnumerable<AvaloniaProperty> GetRegisteredProperties(AvaloniaObject avaloniaObject);

    IEnumerable<AvaloniaProperty> GetRegisteredAttachedProperties(Type ownerType);

    bool IsPropertyRegistered(AvaloniaObject avaloniaObject, AvaloniaProperty property);

    AvaloniaPropertyValue GetDiagnosticValue(AvaloniaObject avaloniaObject, AvaloniaProperty property);

    IEnumerable<IValueFrameDiagnostic> GetAppliedStyleFrames(StyledElement styledElement);
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

    public IEnumerable<AvaloniaProperty> GetRegisteredProperties(AvaloniaObject avaloniaObject)
    {
        return AvaloniaPropertyRegistry.Instance.GetRegistered(avaloniaObject);
    }

    public IEnumerable<AvaloniaProperty> GetRegisteredAttachedProperties(Type ownerType)
    {
        return AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(ownerType);
    }

    public bool IsPropertyRegistered(AvaloniaObject avaloniaObject, AvaloniaProperty property)
    {
        return AvaloniaPropertyRegistry.Instance.IsRegistered(avaloniaObject, property);
    }

    public AvaloniaPropertyValue GetDiagnosticValue(AvaloniaObject avaloniaObject, AvaloniaProperty property)
    {
        return avaloniaObject.GetDiagnostic(property);
    }

    public IEnumerable<IValueFrameDiagnostic> GetAppliedStyleFrames(StyledElement styledElement)
    {
        // Avalonia diagnostics APIs have moved between public, internal, and private
        // surfaces across versions. Keep this as the only boundary for future
        // UnsafeAccessor-first and reflection-fallback implementations.
        return styledElement.GetValueStoreDiagnostic().AppliedFrames;
    }
}
