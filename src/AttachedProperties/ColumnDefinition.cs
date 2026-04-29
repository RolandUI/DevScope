namespace ClassicDiagnostics.Avalonia.AttachedProperties;

/// <summary>
///     See discussion https://github.com/AvaloniaUI/Avalonia/discussions/6773
/// </summary>
internal static class ColumnDefinitionAssist
{
    private readonly static GridLength ZeroWidth = new(0, GridUnitType.Pixel);

    private readonly static AttachedProperty<GridLength?> LastWidthProperty =
        AvaloniaProperty.RegisterAttached<ColumnDefinition, GridLength?>("LastWidth", typeof(ColumnDefinition));

    public readonly static AttachedProperty<bool> IsVisibleProperty =
        AvaloniaProperty.RegisterAttached<ColumnDefinition, bool>(
            "IsVisible",
            typeof(ColumnDefinition),
            true,
            coerce: (element, visibility) =>
            {

                var lastWidth = element.GetValue(LastWidthProperty);
                switch (visibility)
                {
                    case true when lastWidth is not null:
                        element.SetValue(ColumnDefinition.WidthProperty, lastWidth);
                        break;
                    case false:
                        element.SetValue(LastWidthProperty, element.GetValue(ColumnDefinition.WidthProperty));
                        element.SetValue(ColumnDefinition.WidthProperty, ZeroWidth);
                        break;
                }
                return visibility;
            }
        );

    public static bool GetIsVisible(ColumnDefinition columnDefinition)
    {
        return columnDefinition.GetValue(IsVisibleProperty);
    }

    public static void SetIsVisible(ColumnDefinition columnDefinition, bool visibility)
    {
        columnDefinition.SetValue(IsVisibleProperty, visibility);
    }
}