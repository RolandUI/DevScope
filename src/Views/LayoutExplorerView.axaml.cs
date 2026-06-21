using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class LayoutExplorerView : UserControl
{
    private readonly CompositeDisposable _boundsSubscriptions = new();

    public LayoutExplorerView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        SubscribeBoundsUpdates();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // The guideline chain observes several template visuals; release them when this view leaves
        // the visual tree so a stale details panel cannot keep layout controls alive.
        _boundsSubscriptions.Clear();
    }

    private void SubscribeBoundsUpdates()
    {
        _boundsSubscriptions.Clear();

        Visual? visual = ContentArea;
        while (visual != null && !ReferenceEquals(visual, this))
        {
            visual.GetPropertyChangedObservable(BoundsProperty)
                .Subscribe(UpdateSizeGuidelines)
                .DisposeWith(_boundsSubscriptions);
            visual = visual.VisualParent;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void UpdateSizeGuidelines(AvaloniaPropertyChangedEventArgs _)
    {
        void UpdateGuidelines(Visual area)
        {
            // That's what TransformedBounds.Bounds actually was.
            // The code below doesn't really make sense to me, so I've just changed v.TransformedBounds.Bounds
            // to GetPseudoTransformedBounds
            Rect GetPseudoTransformedBounds(Visual v)
            {
                return new Rect(v.Bounds.Size);
            }

            var bounds = GetPseudoTransformedBounds(area);

            {
                // Horizontal guideline
                {
                    if (!TryTranslateToRoot(
                        GetPseudoTransformedBounds(HorizontalSize).BottomLeft,
                        HorizontalSize,
                        out var sizeArea))
                    {
                        return;
                    }

                    if (!TryTranslateToRoot(bounds.BottomLeft, area, out var start))
                    {
                        return;
                    }
                    SetPosition(HorizontalSizeBegin, start);

                    if (!TryTranslateToRoot(bounds.BottomRight, area, out var end))
                    {
                        return;
                    }
                    SetPosition(HorizontalSizeEnd, end.WithX(end.X - 1));

                    var height = ClampLayoutSize(sizeArea.Y - start.Y + 2);
                    HorizontalSizeBegin.Height = height;
                    HorizontalSizeEnd.Height = height;
                }

                // Vertical guideline
                {
                    if (!TryTranslateToRoot(
                        GetPseudoTransformedBounds(VerticalSize).TopRight,
                        VerticalSize,
                        out var sizeArea))
                    {
                        return;
                    }

                    if (!TryTranslateToRoot(bounds.TopRight, area, out var start))
                    {
                        return;
                    }
                    SetPosition(VerticalSizeBegin, start);

                    if (!TryTranslateToRoot(bounds.BottomRight, area, out var end))
                    {
                        return;
                    }
                    SetPosition(VerticalSizeEnd, end.WithY(end.Y - 1));

                    var width = ClampLayoutSize(sizeArea.X - start.X + 2);
                    VerticalSizeBegin.Width = width;
                    VerticalSizeEnd.Width = width;
                }
            }
        }

        bool TryTranslateToRoot(Point point, Visual from, out Point translated)
        {
            translated = default;

            if (from.TranslatePoint(point, LayoutRoot) is not { } result)
            {
                return false;
            }

            if (!double.IsFinite(result.X) || !double.IsFinite(result.Y))
            {
                return false;
            }

            translated = result;
            return true;
        }

        static void SetPosition(Rectangle rect, Point start)
        {
            Canvas.SetLeft(rect, start.X);
            Canvas.SetTop(rect, start.Y);
        }

        if (BorderArea.IsPresent)
        {
            UpdateGuidelines(BorderArea);
        }
        else if (PaddingArea.IsPresent)
        {
            UpdateGuidelines(PaddingArea);
        }
        else
        {
            UpdateGuidelines(ContentArea);
        }
    }

    private static double ClampLayoutSize(double value)
    {
        return double.IsFinite(value) && value > 0 ? value : 0;
    }
}
