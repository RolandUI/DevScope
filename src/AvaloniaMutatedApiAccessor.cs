namespace ClassicDiagnostics.Avalonia;

internal static class AvaloniaMutatedApiAccessor
{
    private static readonly Func<PresentationSource, Point, PixelPoint?> PointToScreenFunc;

    static AvaloniaMutatedApiAccessor()
    {
        var pointToScreenMethodInfo = typeof(PresentationSource).GetMethod("PointToScreen", [typeof(Point)]);
        if (pointToScreenMethodInfo is null)
        {
            throw new InvalidOperationException("Could not find PointToScreen method on PresentationSource.");
        }

        if (pointToScreenMethodInfo.ReturnType == typeof(PixelPoint))
        {
            var @delegate = pointToScreenMethodInfo.CreateDelegate<Func<PresentationSource, Point, PixelPoint>>();
            PointToScreenFunc = (source, point) => @delegate(source, point);
        }
        else if (pointToScreenMethodInfo.ReturnType == typeof(PixelPoint?))
        {
            PointToScreenFunc = pointToScreenMethodInfo.CreateDelegate<Func<PresentationSource, Point, PixelPoint?>>();
        }
        else
        {
            throw new InvalidOperationException("Unexpected return type for PointToScreen method on PresentationSource.");
        }
    }

    /// <summary>
    /// Avalonia changes the return type of PresentationSource.PointToScreen() from PixelPoint to PixelPoint? in 0.12.4
    /// </summary>
    /// <param name="source"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static PixelPoint? PointToScreen(PresentationSource source, Point point) => PointToScreenFunc.Invoke(source, point);
}