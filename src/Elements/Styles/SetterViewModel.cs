using Avalonia.Input.Platform;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Styles;

internal class SetterViewModel : ViewModelBase
{
    public AvaloniaProperty Property { get; }

    public string Name { get; }

    public object? Value { get; }

    public bool IsActive
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsVisible
    {
        get;
        set => SetProperty(ref field, value);
    }

    private readonly IClipboard? _clipboard;

    public SetterViewModel(AvaloniaProperty property, object? value, IClipboard? clipboard)
    {
        Property = property;
        Name = property.Name;
        Value = value;
        IsActive = true;
        IsVisible = true;

        _clipboard = clipboard;
    }

    public virtual void CopyValue()
    {
        var textToCopy = Value?.ToString();

        if (textToCopy is null)
        {
            return;
        }

        CopyToClipboard(textToCopy);
    }

    public void CopyPropertyName()
    {
        CopyToClipboard(Property.Name);
    }

    protected void CopyToClipboard(string value)
    {
        CopyToClipboardAsync(value).Detach($"Failed to copy value '{value}' to clipboard.");
    }

    private async Task CopyToClipboardAsync(string value)
    {
        if (_clipboard is null)
        {
            return;
        }

        await _clipboard.SetTextAsync(value);
    }
}