namespace ClassicDiagnostics.Avalonia.Models;

/// <summary>
///     Description of a hotkey, including the gesture, a brief description, and an optional detailed description
/// </summary>
/// <param name="Gesture"></param>
/// <param name="BriefDescription"></param>
/// <param name="DetailedDescription"></param>
internal record HotKeyDescription(string Gesture, string BriefDescription, string? DetailedDescription = null);