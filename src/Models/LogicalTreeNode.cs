using Avalonia.Collections;
using Avalonia.LogicalTree;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Views;

namespace ClassicDiagnostics.Avalonia.Models;

internal class LogicalTreeNode : TreeNode
{
    public LogicalTreeNode(AvaloniaObject avaloniaObject, TreeNode? parent)
        : base(avaloniaObject, parent)
    {
        Children = avaloniaObject switch
        {
            ILogical logical => new LogicalTreeNodeCollection(this, logical),
            TopLevelGroup host => new TopLevelGroupHostLogical(this, host),
            _ => TreeNodeCollection.Empty,
        };
    }

    public override TreeNodeCollection Children { get; }

    public static LogicalTreeNode[] Create(object control)
    {
        return control is AvaloniaObject logical ? [new LogicalTreeNode(logical, null)] : [];
    }

    internal class LogicalTreeNodeCollection(TreeNode owner, ILogical control) : TreeNodeCollection(owner)
    {
        private IDisposable? _subscription;

        public override void Dispose()
        {
            base.Dispose();
            _subscription?.Dispose();
        }

        protected override void Initialize(AvaloniaList<TreeNode> nodes)
        {
            _subscription = control.LogicalChildren.ForEachItem(
                (i, item) => nodes.Insert(i, new LogicalTreeNode((AvaloniaObject)item, Owner)),
                (i, _) => nodes.RemoveAt(i),
                nodes.Clear);
        }
    }

    internal class TopLevelGroupHostLogical(TreeNode owner, TopLevelGroup group) : TreeNodeCollection(owner)
    {
        private readonly List<IDisposable> _subscriptions = [];

        protected override void Initialize(AvaloniaList<TreeNode> nodes)
        {
            foreach (var window in group.Items)
            {
                if (window is MainWindow) continue;

                nodes.Add(new LogicalTreeNode(window, Owner));
            }

            void GroupOnAdded(object? sender, TopLevel e)
            {
                if (e is MainWindow) return;

                nodes.Add(new LogicalTreeNode(e, Owner));
            }

            void GroupOnRemoved(object? sender, TopLevel e)
            {
                if (e is MainWindow) return;

                var node = nodes.FirstOrDefault(x => ReferenceEquals(x.Visual, e));
                if (node is not null)
                {
                    nodes.Remove(node);
                    node.Dispose();
                }
            }

            group.Added += GroupOnAdded;
            group.Removed += GroupOnRemoved;

            Disposable.Create(() =>
                {
                    group.Added -= GroupOnAdded;
                    group.Removed -= GroupOnRemoved;
                })
                .AddTo(_subscriptions);
        }

        public override void Dispose()
        {
            foreach (var disposable in _subscriptions)
            {
                disposable.Dispose();
            }
            _subscriptions.Clear();

            base.Dispose();
        }
    }
}
