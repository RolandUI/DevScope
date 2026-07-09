using Avalonia.Input.Platform;
using Avalonia.Media;
using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Elements.Styles;

internal class ResourceSetterViewModel(
    AvaloniaProperty property,
    object resourceKey,
    object? resourceValue,
    bool isDynamic,
    IClipboard? clipboard
) : SetterViewModel(property, resourceValue, clipboard)
{
    public object Key { get; } = resourceKey;

    public IBrush Tint { get; } = isDynamic ? Brushes.Orange : Brushes.Brown;

    public string ValueTypeTooltip { get; } = isDynamic ? "Dynamic Resource" : "Static Resource";

    public void CopyResourceKey()
    {
        var textToCopy = Key.ToString();
        if (textToCopy is null)
        {
            return;
        }

        CopyToClipboard(textToCopy);
    }
}