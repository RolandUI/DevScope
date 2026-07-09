using Avalonia.Collections;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using RolandUI.DevScope.Rooting;
using RolandUI.DevScope.Views.Shell;

namespace RolandUI.DevScope.Elements.Trees;

internal interface ILogicalTreeProvider
{
    IReadOnlyList<TreeNodeModel> Create(AvaloniaObject root);
}

internal interface IVisualTreeProvider
{
    IReadOnlyList<TreeNodeModel> Create(AvaloniaObject root);
}

internal sealed class LogicalTreeProvider : ILogicalTreeProvider
{
    public IReadOnlyList<TreeNodeModel> Create(AvaloniaObject root)
    {
        return [CreateNode(root, null)];
    }

    private TreeNodeModel CreateNode(AvaloniaObject target, TreeNodeModel? parent)
    {
        return new TreeNodeModel(
            target,
            parent,
            owner => target switch
            {
                ILogical logical => new LogicalChildrenCollection(owner, logical, this),
                PresentationRootNode host => new PresentationRootLogicalCollection(owner, host, this),
                _ => TreeNodeModelCollection.Empty,
            });
    }

    private sealed class LogicalChildrenCollection(
        TreeNodeModel owner,
        ILogical target,
        LogicalTreeProvider provider
    ) : TreeNodeModelCollection(owner)
    {
        private IDisposable? _subscription;

        public override void Dispose()
        {
            _subscription?.Dispose();
            base.Dispose();
        }

        protected override void Initialize(AvaloniaList<TreeNodeModel> nodes)
        {
            _subscription = target.LogicalChildren.ForEachItem(
                (i, item) => nodes.Insert(i, provider.CreateNode((AvaloniaObject)item, Owner!)),
                (i, _) => RemoveAndDisposeAt(nodes, i),
                () => DisposeAndClear(nodes));
        }
    }

    private sealed class PresentationRootLogicalCollection(
        TreeNodeModel owner,
        PresentationRootNode group,
        LogicalTreeProvider provider
    ) : TreeNodeModelCollection(owner)
    {
        private readonly CompositeDisposable _subscriptions = new(1);

        public override void Dispose()
        {
            _subscriptions.Dispose();
            base.Dispose();
        }

        protected override void Initialize(AvaloniaList<TreeNodeModel> nodes)
        {
            _subscriptions.Clear();

            foreach (var root in group.Items)
            {
                AddRoot(nodes, root);
            }

            void HandleGroupAdded(object? sender, TopLevel e)
            {
                AddRoot(nodes, e);
            }

            void HandleGroupRemoved(object? sender, TopLevel e)
            {
                if (e is MainWindow)
                {
                    return;
                }

                var node = nodes.FirstOrDefault(x => ReferenceEquals(x.Target, e));
                if (node is not null)
                {
                    RemoveAndDispose(nodes, node);
                }
            }

            group.Added += HandleGroupAdded;
            group.Removed += HandleGroupRemoved;

            Disposable.Create(() =>
                {
                    group.Added -= HandleGroupAdded;
                    group.Removed -= HandleGroupRemoved;
                })
                .AddTo(_subscriptions);
        }

        private void AddRoot(AvaloniaList<TreeNodeModel> nodes, TopLevel root)
        {
            if (root is MainWindow)
            {
                return;
            }

            nodes.Add(provider.CreateNode(root, Owner!));
        }
    }
}

internal sealed class VisualTreeProvider : IVisualTreeProvider
{
    public IReadOnlyList<TreeNodeModel> Create(AvaloniaObject root)
    {
        return [CreateNode(root, null)];
    }

    private TreeNodeModel CreateNode(AvaloniaObject target, TreeNodeModel? parent, string? customName = null)
    {
        return new TreeNodeModel(
            target,
            parent,
            owner => target switch
            {
                Visual visual => new VisualChildrenCollection(owner, visual, this),
                PresentationRootNode host => new PresentationRootVisualCollection(owner, host, this),
                _ => TreeNodeModelCollection.Empty,
            },
            customName);
    }

    private sealed class VisualChildrenCollection(
        TreeNodeModel owner,
        Visual target,
        VisualTreeProvider provider
    ) : TreeNodeModelCollection(owner)
    {
        private readonly CompositeDisposable _subscriptions = new(2);

        public override void Dispose()
        {
            _subscriptions.Dispose();
            base.Dispose();
        }

        protected override void Initialize(AvaloniaList<TreeNodeModel> nodes)
        {
            _subscriptions.Clear();

            if (GetHostedPopupRootObservable(target) is { } popupRootObservable)
            {
                TreeNodeModel? childNode = null;

                popupRootObservable
                    .Subscribe(popupRoot =>
                    {
                        if (popupRoot is not null)
                        {
                            childNode = provider.CreateNode(
                                popupRoot.Value.Root,
                                Owner,
                                popupRoot.Value.CustomName);

                            nodes.Add(childNode);
                        }
                        else if (childNode is not null)
                        {
                            RemoveAndDispose(nodes, childNode);
                            childNode = null;
                        }
                    })
                    .AddTo(_subscriptions);
            }

            target.VisualChildren.ForEachItem(
                    (i, item) => nodes.Insert(i, provider.CreateNode(item, Owner!)),
                    (i, _) => RemoveAndDisposeAt(nodes, i),
                    () => DisposeAndClear(nodes))
                .AddTo(_subscriptions);
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
                        {
                            return new PopupRoot(
                                control,
                                providerName != null ? $"{providerName} ({control.GetType().Name})" : null);
                        }

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
                        {
                            // ContextMenus are special because their items are already visual children.
                            return Observable.Return<PopupRoot?>(new PopupRoot(contextMenu));
                        }

                        if (contextFlyout != null)
                        {
                            return GetPopupHostObservable(contextFlyout, "ContextFlyout");
                        }

                        if (attachedFlyout != null)
                        {
                            return GetPopupHostObservable(attachedFlyout, "AttachedFlyout");
                        }

                        if (toolTip != null)
                        {
                            return GetPopupHostObservable(toolTip, "ToolTip");
                        }

                        return buttonFlyout != null ?
                            GetPopupHostObservable(buttonFlyout, "Flyout") :
                            Observable.Return<PopupRoot?>(null);
                    })
                    .Switch(),
                _ => null,
            };
        }

        private readonly record struct PopupRoot(Control Root, string? CustomName = null);
    }

    private sealed class PresentationRootVisualCollection(
        TreeNodeModel owner,
        PresentationRootNode group,
        VisualTreeProvider provider
    ) : TreeNodeModelCollection(owner)
    {
        private readonly CompositeDisposable _subscriptions = new(1);

        public override void Dispose()
        {
            _subscriptions.Dispose();
            base.Dispose();
        }

        protected override void Initialize(AvaloniaList<TreeNodeModel> nodes)
        {
            _subscriptions.Clear();

            foreach (var root in group.Items)
            {
                AddRoot(nodes, root);
            }

            void HandleGroupAdded(object? sender, TopLevel e)
            {
                AddRoot(nodes, e);
            }

            void HandleGroupRemoved(object? sender, TopLevel e)
            {
                if (e is MainWindow)
                {
                    return;
                }

                var node = nodes.FirstOrDefault(x => ReferenceEquals(x.Target, e));
                if (node is not null)
                {
                    RemoveAndDispose(nodes, node);
                }
            }

            group.Added += HandleGroupAdded;
            group.Removed += HandleGroupRemoved;

            Disposable.Create(() =>
                {
                    group.Added -= HandleGroupAdded;
                    group.Removed -= HandleGroupRemoved;
                })
                .AddTo(_subscriptions);
        }

        private void AddRoot(AvaloniaList<TreeNodeModel> nodes, TopLevel root)
        {
            if (root is MainWindow)
            {
                return;
            }

            nodes.Add(provider.CreateNode(root, Owner!));
        }
    }
}
