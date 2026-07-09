using Avalonia.Controls.Primitives;
using RolandUI.DevScope.Views.Shell;

namespace RolandUI.DevScope.Extensions;

internal static class DevToolsExtensions
{
    /// <summary>
    /// Determines whether the specified visual belongs to the DevTools window.
    /// </summary>
    /// <param name="visual"></param>
    /// <returns></returns>
    public static bool DoesBelongToDevTool(this Visual visual)
    {
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

        return true;
    }
}