using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Models;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Views;

internal partial class TreePageView : UserControl
{
    private readonly TreeView _tree;
    private IDisposable? _adorner;
    private TreeViewItem? _hovered;

    public TreePageView()
    {
        InitializeComponent();
        _tree = this.GetControl<TreeView>("tree");
    }

    protected void UpdateAdorner(object? sender, PointerEventArgs e)
    {
        if (e.Source is not StyledElement source)
        {
            return;
        }

        var item = source.FindLogicalAncestorOfType<TreeViewItem>();
        if (item == _hovered)
        {
            return;
        }

        _adorner?.Dispose();

        if (item is null || item.TreeViewOwner != _tree)
        {
            _hovered = null;
            return;
        }

        _hovered = item;

        var target = (item.DataContext as TreeNode)?.Visual;
        var shouldVisualizeMarginPadding = (DataContext as TreePageViewModel)?.MainView.ShouldVisualizeMarginPadding;
        if (target is null || shouldVisualizeMarginPadding is null)
        {
            return;
        }

        _adorner = AddHighlight(target, shouldVisualizeMarginPadding == true);
    }

    private void RemoveAdorner(object? sender, PointerEventArgs e)
    {
        _adorner?.Dispose();
        _adorner = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DataContextProperty)
        {
            if (change.GetOldValue<object?>() is TreePageViewModel oldViewModel)
                oldViewModel.ClipboardCopyRequested -= OnClipboardCopyRequested;
            if (change.GetNewValue<object?>() is TreePageViewModel newViewModel)
                newViewModel.ClipboardCopyRequested += OnClipboardCopyRequested;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnClipboardCopyRequested(object? sender, string selector)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            var dataTransferItem = new DataTransferItem();
            dataTransferItem.SetText(ToText(selector));
            dataTransferItem.Set(DevToolsDataFormats.Selector, selector);

            var dataTransfer = new DataTransfer();
            dataTransfer.Add(dataTransferItem);
            clipboard.SetDataAsync(dataTransfer);
        }
    }

    private static string ToText(string text)
    {
        var sb = new StringBuilder();
        var bufferStartIndex = -1;
        foreach (var c in text)
        {
            switch (c)
            {
                case '{':
                    bufferStartIndex = sb.Length;
                    break;
                case '}' when bufferStartIndex > -1:
                    sb.Remove(bufferStartIndex, sb.Length - bufferStartIndex);
                    bufferStartIndex = sb.Length;
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private IBrush GetHighlightBrush()
    {
        return (DataContext as TreePageViewModel)?.MainView.FocusHighlighter ?? Brushes.Red;
    }

    private IDisposable? AddHighlight(AvaloniaObject target, bool shouldVisualizeMarginPadding)
    {
        if (target is TopLevel topLevel)
        {
            return AddTopLevelHighlight(topLevel, shouldVisualizeMarginPadding);
        }

        return target is Visual visual ?
            ControlHighlightAdorner.Add(visual, shouldVisualizeMarginPadding) :
            null;
    }

    private IDisposable? AddTopLevelHighlight(TopLevel topLevel, bool shouldVisualizeMarginPadding)
    {
        // TopLevel itself is a presentation root; its adorned bounds are often not the
        // useful client area. Prefer the hosted content so hovering a Window/TopLevel node
        // highlights the same object the user actually sees.
        if (TryGetTopLevelContent(topLevel) is { } content)
        {
            var adorner = ControlHighlightAdorner.Add(content, shouldVisualizeMarginPadding);
            if (adorner is not null)
            {
                return adorner;
            }
        }

        return ControlHighlightAdorner.Add(topLevel, GetHighlightBrush());
    }

    private static Visual? TryGetTopLevelContent(TopLevel topLevel)
    {
        if (topLevel is ContentControl { Content: Visual content })
        {
            return content;
        }

        return topLevel.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(control => control is not Window);
    }
}
