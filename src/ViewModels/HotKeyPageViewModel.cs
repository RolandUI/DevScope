using System.Collections.ObjectModel;
using ClassicDiagnostics.Avalonia.Models;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class HotKeyPageViewModel : ViewModelBase
{
    public ObservableCollection<HotKeyDescription>? HotKeyDescriptions
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public void SetOptions(DevToolsOptions options)
    {
        var hotKeys = options.HotKeys;

        HotKeyDescriptions = new ObservableCollection<HotKeyDescription>
        {
            new(CreateDescription(options.Gesture), "Launch DevTools", "Launches DevTools to inspect the TopLevel that received the hotkey input"),
            new(
                CreateDescription(hotKeys.ValueFramesFreeze),
                "Freeze Value Frames",
                "Pauses refreshing the Value Frames inspector for the selected Control"),
            new(
                CreateDescription(hotKeys.ValueFramesUnfreeze),
                "Unfreeze Value Frames",
                "Resumes refreshing the Value Frames inspector for the selected Control"),
            new(
                CreateDescription(hotKeys.InspectHoveredControl),
                "Inspect Control Under Pointer",
                "Inspects the hovered Control in the Logical or Visual Tree Page"),
            new(CreateDescription(hotKeys.TogglePopupFreeze), "Toggle Popup Freeze", "Prevents visible Popups from closing so they can be inspected"),
            new(
                CreateDescription(hotKeys.ScreenshotSelectedControl),
                "Screenshot Selected Control",
                "Saves a Screenshot of the Selected Control in the Logical or Visual Tree Page"),
        };
    }

    private string CreateDescription(KeyGesture gesture)
    {
        if (gesture.Key == Key.None && gesture.KeyModifiers != KeyModifiers.None)
        {
            return gesture.ToString().Replace("+None", "");
        }

        return gesture.ToString();
    }
}
