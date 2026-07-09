using Avalonia.Controls.Primitives;
using RolandUI.DevScope.Views.Shell;

namespace RolandUI.DevScope.Extensions;

internal interface IDevToolsVisual
{
}

internal static class DevToolsExtensions
{
    /// <summary>
    /// Determines whether the specified visual belongs to the DevTools window.
    /// </summary>
    /// <param name="visual"></param>
    /// <returns></returns>
    public static bool DoesBelongToDevTool(this Visual visual)
    {
        for (Visual? current = visual; current is not null; current = current.VisualParent)
        {
            if (current is IDevToolsVisual)
            {
                return true;
            }

            if (current is PopupRoot { Parent: Popup { PlacementTarget: Visual placementTarget } }
                && !ReferenceEquals(placementTarget, visual)
                && placementTarget.DoesBelongToDevTool())
            {
                return true;
            }
        }

        var topLevel = TopLevel.GetTopLevel(visual);
        while (topLevel is not null && topLevel is not MainWindow)
        {
            if (topLevel is PopupRoot popupRoot)
            {
                topLevel = popupRoot.ParentTopLevel;
            }
            else
            {
                return false;
            }
        }

        return topLevel is MainWindow;
    }
}
