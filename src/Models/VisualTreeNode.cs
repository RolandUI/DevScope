using Avalonia.Collections;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Primitives;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Views;

namespace ClassicDiagnostics.Avalonia.Models;

internal class VisualTreeNode : TreeNode
{
    public VisualTreeNode(AvaloniaObject avaloniaObject, TreeNode? parent, string? customName = null)
        : base(avaloniaObject, parent, customName)
    {
        Children = avaloniaObject switch
        {
            Visual visual => new VisualTreeNodeCollection(this, visual),
            PresentationRootGroup host => new PresentationRootHostVisuals(this, host),
            _ => TreeNodeCollection.Empty,
        };

        if (Visual is StyledElement styleable)
            IsInTemplate = styleable.TemplatedParent != null;
    }

    public bool IsInTemplate { get; }

    public override TreeNodeCollection Children { get; }

    public static IReadOnlyList<VisualTreeNode> Create(object control)
    {
        return control is AvaloniaObject visual ? [new VisualTreeNode(visual, null)] : [];
    }

    private sealed class VisualTreeNodeCollection(TreeNode owner, Visual control) : TreeNodeCollection(owner)
    {
        private readonly CompositeDisposable _subscriptions = new(2);

        public override void Dispose()
        {
            _subscriptions.Dispose();
        }

        private static IObservable<PopupRoot?>? GetHostedPopupRootObservable(Visual visual)
        {
            static IObservable<PopupRoot?> GetPopupHostObservable(
                IPopupHostProvider popupHostProvider,
                string? providerName = null)
            {
                return Observable.Create<IPopupHost?>(observer =>
                    {
                        void Handler(IPopupHost? args)
                        {
                            observer.OnNext(args);
                        }

                        popupHostProvider.PopupHostChanged += Handler;
                        return Disposable.Create(() => popupHostProvider.PopupHostChanged -= Handler);
                    })
                    .StartWith(popupHostProvider.PopupHost)
                    .Select(popupHost =>
                    {
                        if (popupHost is Control control)
                            return new PopupRoot(
                                control,
                                providerName != null ? $"{providerName} ({control.GetType().Name})" : null);

                        return (PopupRoot?)null;
                    });
            }

            return visual switch
            {
                Popup p => GetPopupHostObservable(p),
                Control c => new IObservable<object?>[]
                    {
                        c.GetObservable(Control.ContextFlyoutProperty),
                        c.GetObservable(Control.ContextMenuProperty),
                        c.GetObservable(FlyoutBase.AttachedFlyoutProperty),
                        c.GetObservable(ToolTipDiagnostics.ToolTipProperty),
                        c.GetObservable(Button.FlyoutProperty),
                    }.CombineLatest()
                    .Select(items =>
                    {
                        var contextFlyout = items[0] as IPopupHostProvider;
                        var contextMenu = items[1] as ContextMenu;
                        var attachedFlyout = items[2] as IPopupHostProvider;
                        var toolTip = items[3] as IPopupHostProvider;
                        var buttonFlyout = items[4] as IPopupHostProvider;

                        if (contextMenu != null)
                            //Note: ContextMenus are special since all the items are added as visual children.
                            //So we don't need to go via Popup
                            return Observable.Return<PopupRoot?>(new PopupRoot(contextMenu));

                        if (contextFlyout != null)
                            return GetPopupHostObservable(contextFlyout, "ContextFlyout");

                        if (attachedFlyout != null)
                            return GetPopupHostObservable(attachedFlyout, "AttachedFlyout");

                        if (toolTip != null)
                            return GetPopupHostObservable(toolTip, "ToolTip");

                        if (buttonFlyout != null)
                            return GetPopupHostObservable(buttonFlyout, "Flyout");

                        return Observable.Return<PopupRoot?>(null);
                    })
                    .Switch(),
                _ => null,
            };
        }

        protected override void Initialize(AvaloniaList<TreeNode> nodes)
        {
            _subscriptions.Clear();

            if (GetHostedPopupRootObservable(control) is { } popupRootObservable)
            {
                VisualTreeNode? childNode = null;

                popupRootObservable
                    .Subscribe(popupRoot =>
                    {
                        if (popupRoot != null)
                        {
                            childNode = new VisualTreeNode(
                                popupRoot.Value.Root,
                                Owner,
                                popupRoot.Value.CustomName);

                            nodes.Add(childNode);
                        }
                        else if (childNode != null)
                        {
                            nodes.Remove(childNode);
                        }
                    })
                    .AddTo(_subscriptions);
            }

            control.VisualChildren.ForEachItem(
                    (i, item) => nodes.Insert(i, new VisualTreeNode(item, Owner)),
                    (i, _) => nodes.RemoveAt(i),
                    nodes.Clear)
                .AddTo(_subscriptions);
        }

        private readonly record struct PopupRoot(Control Root, string? CustomName = null);
    }

    private sealed class PresentationRootHostVisuals(TreeNode owner, PresentationRootGroup host) : TreeNodeCollection(owner)
    {
        private readonly CompositeDisposable _subscriptions = new(2);

        protected override void Initialize(AvaloniaList<TreeNode> nodes)
        {
            _subscriptions.Clear();

            foreach (var root in host.Items)
            {
                AddRoot(nodes, root);
            }

            void GroupOnAdded(object? sender, TopLevel e)
            {
                AddRoot(nodes, e);
            }

            void GroupOnRemoved(object? sender, TopLevel e)
            {
                if (e is MainWindow) return;

                var item = nodes.FirstOrDefault(node => ReferenceEquals(node.Visual, e));
                if (item is not null)
                {
                    nodes.Remove(item);
                    item.Dispose();
                }
            }

            host.Added += GroupOnAdded;
            host.Removed += GroupOnRemoved;

            Disposable.Create(() =>
                {
                    host.Added -= GroupOnAdded;
                    host.Removed -= GroupOnRemoved;
                })
                .AddTo(_subscriptions);
        }

        private void AddRoot(AvaloniaList<TreeNode> nodes, TopLevel root)
        {
            if (root is MainWindow)
            {
                return;
            }

            nodes.Add(new VisualTreeNode(root, Owner));
        }

        public override void Dispose()
        {
            _subscriptions.Dispose();
            base.Dispose();
        }
    }
}
