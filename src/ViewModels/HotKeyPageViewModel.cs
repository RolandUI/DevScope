using System.Collections.ObjectModel;
using RolandUI.DevScope.Models;

namespace RolandUI.DevScope.ViewModels;

internal class HotKeyPageViewModel : ReactiveViewModelBase
{
    public ObservableCollection<HotKeyDescription>? HotKeyDescriptions
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public void SetOptions(DevToolsOptions options)
    {
        var hotKeys = options.HotKeys;

        HotKeyDescriptions =
        [
            new HotKeyDescription(
                CreateDescription(options.Gesture),
                "Launch DevTools",
                "Launches DevTools to inspect the TopLevel that received the hotkey input"),
            new HotKeyDescription(
                CreateDescription(hotKeys.ValueFramesFreeze),
                "Freeze Value Frames",
                "Pauses refreshing the Value Frames inspector for the selected Control"),
            new HotKeyDescription(
                CreateDescription(hotKeys.ValueFramesUnfreeze),
                "Unfreeze Value Frames",
                "Resumes refreshing the Value Frames inspector for the selected Control"),
            new HotKeyDescription(
                CreateDescription(hotKeys.InspectHoveredControl),
                "Inspect Control Under Pointer",
                "Inspects the hovered Control in the Logical or Visual Tree Page"),
            new HotKeyDescription(
                CreateDescription(hotKeys.TogglePopupFreeze),
                "Toggle Popup Freeze",
                "Prevents visible Popups from closing so they can be inspected"),
            new HotKeyDescription(
                CreateDescription(hotKeys.ScreenshotSelectedControl),
                "Screenshot Selected Control",
                "Saves a Screenshot of the Selected Control in the Logical or Visual Tree Page"),
        ];
    }

    private static string CreateDescription(KeyGesture gesture)
    {
        if (gesture.Key == Key.None && gesture.KeyModifiers != KeyModifiers.None)
        {
            return gesture.ToString().Replace("+None", "");
        }

        return gesture.ToString();
    }
}