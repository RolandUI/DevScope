using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ClassicDiagnostics.Avalonia.Extensions;

internal static class VisualExtensions
{
    /// <summary>
    ///     Render control to the destination stream.
    /// </summary>
    /// <param name="source">Control to be rendered.</param>
    /// <param name="destination">Destination stream.</param>
    /// <param name="dpi">Dpi quality.</param>
    public static void RenderTo(this Control source, Stream destination, double dpi = 96)
    {
        var transform = source.CompositionVisual?.TransformMatrix;
        if (transform is null)
            return;

        var rect = new Rect(source.Bounds.Size).TransformToAABB(transform.Value);
        var top = rect.TopLeft;
        var pixelSize = new PixelSize((int)rect.Width, (int)rect.Height);
        var dpiVector = new Vector(dpi, dpi);

        // get Visual root
        var root = (source.VisualRoot
            ?? TopLevel.GetTopLevel(source))
            as Control ?? source;

        IDisposable? clipSetter = null;
        IDisposable? clipToBoundsSetter = null;
        IDisposable? renderTransformOriginSetter = null;
        IDisposable? renderTransformSetter = null;
        try
        {
            // Set clip region
            var clipRegion = new RectangleGeometry(rect);
            clipToBoundsSetter = root.SetValue(Visual.ClipToBoundsProperty, true, BindingPriority.Animation);
            clipSetter = root.SetValue(Visual.ClipProperty, clipRegion, BindingPriority.Animation);

            // Translate origin
            renderTransformOriginSetter = root.SetValue(Visual.RenderTransformOriginProperty,
                new RelativePoint(top, RelativeUnit.Absolute),
                BindingPriority.Animation);

            renderTransformSetter = root.SetValue(Visual.RenderTransformProperty,
                new TranslateTransform(-top.X, -top.Y),
                BindingPriority.Animation);

            using (var bitmap = new RenderTargetBitmap(pixelSize, dpiVector))
            {
                bitmap.Render(root);
                bitmap.Save(destination, PngBitmapEncoderOptions.Default);
            }
        }
        finally
        {
            // Restore values before transformation
            renderTransformSetter?.Dispose();
            renderTransformOriginSetter?.Dispose();
            clipSetter?.Dispose();
            clipToBoundsSetter?.Dispose();
            source.InvalidateVisual();
        }
    }
}
