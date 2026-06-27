using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using ClassicDiagnostics.Avalonia.Elements.Trees;
using ClassicDiagnostics.Avalonia.Views.Controls;

namespace ClassicDiagnostics.Avalonia.Views.Elements;

internal partial class ElementsTreeView : ReactiveUserControl<ElementsTreeViewModel>
{
    private IDisposable? _adorner;
    private TreeViewItem? _hovered;

    public ElementsTreeView(ElementsTreeViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }

    protected void HandleTreePointerMoved(object? sender, PointerEventArgs e)
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

        if (item is null || item.TreeViewOwner != Tree)
        {
            _hovered = null;
            return;
        }

        _hovered = item;

        if (item.DataContext is not TreeNodeViewModel treeNodeViewModel)
        {
            return;
        }

        _adorner = AddHighlight(treeNodeViewModel.Model.Target, RequiredViewModel.MainView.ShouldVisualizeMarginPadding);
    }

    private void HandleTreePointerExited(object? sender, PointerEventArgs e)
    {
        _adorner?.Dispose();
        _adorner = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DataContextProperty)
        {
            if (change.GetOldValue<object?>() is ElementsTreeViewModel oldViewModel)
                oldViewModel.ClipboardCopyRequested -= HandleClipboardCopyRequested;
            if (change.GetNewValue<object?>() is ElementsTreeViewModel newViewModel)
                newViewModel.ClipboardCopyRequested += HandleClipboardCopyRequested;
        }
    }

    private void HandleClipboardCopyRequested(object? sender, string selector)
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
        return RequiredViewModel.MainView.FocusHighlighter ?? Brushes.Red;
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