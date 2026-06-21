using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace ClassicDiagnostics.Avalonia.Controls;

internal class ControlHighlightAdorner : Control
{
    private readonly static Panel LayoutHighlightAdorner;

    private readonly IPen _pen;

    static ControlHighlightAdorner()
    {
        LayoutHighlightAdorner = new Panel
        {
            ClipToBounds = false,
            Children =
            {
                // Padding frame
                new Border { BorderBrush = new SolidColorBrush(Colors.Green, 0.5) },
                // Content frame
                new Border { Background = new SolidColorBrush(Color.FromRgb(160, 197, 232), 0.5) },
                // Margin frame
                new Border { BorderBrush = new SolidColorBrush(Colors.Yellow, 0.5) },
            },
        };
        AdornerLayer.SetIsClipEnabled(LayoutHighlightAdorner, false);
    }

    private ControlHighlightAdorner(IPen pen)
    {
        _pen = pen;
        Clip = null;
    }

    public static IDisposable? Add(InputElement owner, IBrush highlightBrush)
    {

        if (AdornerLayer.GetAdornerLayer(owner) is { } layer)
        {
            var pen = new Pen(highlightBrush, 2).ToImmutable();
            var adorner = new ControlHighlightAdorner(pen)
            {
                [AdornerLayer.AdornedElementProperty] = owner,
            };
            layer.Children.Add(adorner);

            return Disposable.Create(
                (layer, adorner),
                state =>
                {
                    state.layer.Children.Remove(state.adorner);
                });
        }

        return null;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(_pen, Bounds.Deflate(2));
    }

    internal static IDisposable? Add(Visual visual, bool visualizeMarginPadding)
    {
        if (AdornerLayer.GetAdornerLayer(visual) is not { } layer) return null;
        if (layer.Children.Contains(LayoutHighlightAdorner)) return null;

        layer.Children.Add(LayoutHighlightAdorner);
        AdornerLayer.SetAdornedElement(LayoutHighlightAdorner, visual);
        var paddingBorder = (Border)LayoutHighlightAdorner.Children[0];
        var contentBorder = (Border)LayoutHighlightAdorner.Children[1];
        var marginBorder = (Border)LayoutHighlightAdorner.Children[2];
        if (visualizeMarginPadding)
        {
            paddingBorder.BorderThickness = visual.GetValue(TemplatedControl.PaddingProperty);
            contentBorder.Margin = visual.GetValue(TemplatedControl.PaddingProperty);
            marginBorder.BorderThickness = visual.GetValue(MarginProperty);
            marginBorder.Margin = InvertThickness(visual.GetValue(MarginProperty));
        }
        else
        {
            paddingBorder.BorderThickness = default;
            contentBorder.Margin = default;
            marginBorder.BorderThickness = default;
            marginBorder.Margin = default;
        }
        return Disposable.Create(
            (Layer: layer, Adorner: LayoutHighlightAdorner),
            state =>
            {
                state.Layer.Children.Remove(state.Adorner);
            });
    }


    private static Thickness InvertThickness(Thickness input)
    {
        return new Thickness(-input.Left, -input.Top, -input.Right, -input.Bottom);
    }
}