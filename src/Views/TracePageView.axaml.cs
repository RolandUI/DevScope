using System.Collections.Specialized;
using Avalonia.Threading;
using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Views;

internal partial class TracePageView : ReactiveUserControl<TracePageViewModel>
{
    private bool _isSubscribed;

    public TracePageView(TracePageViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (!_isSubscribed)
        {
            RequiredViewModel.Entries.CollectionChanged += EntriesCollectionChanged;
            _isSubscribed = true;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_isSubscribed)
        {
            RequiredViewModel.Entries.CollectionChanged -= EntriesCollectionChanged;
            _isSubscribed = false;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void EntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!RequiredViewModel.AutoScroll || e.Action != NotifyCollectionChangedAction.Add)
        {
            return;
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                if (RequiredViewModel.AutoScroll && RequiredViewModel.Entries.LastOrDefault() is { } last)
                {
                    TraceGrid.ScrollIntoView(last, TraceGrid.Columns[0]);
                }
            },
            DispatcherPriority.Background);
    }
}
