using Avalonia.Collections;
using Avalonia.LogicalTree;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Views;

namespace ClassicDiagnostics.Avalonia.ViewModels
{
    internal class LogicalTreeNode : TreeNode
    {
        public LogicalTreeNode(AvaloniaObject avaloniaObject, TreeNode? parent)
            : base(avaloniaObject, parent)
        {
            Children =  avaloniaObject switch
            {
                ILogical logical => new LogicalTreeNodeCollection(this, logical),
                TopLevelGroup host => new TopLevelGroupHostLogical(this, host),
                _ => TreeNodeCollection.Empty
            };
        }

        public override TreeNodeCollection Children { get; }

        public static LogicalTreeNode[] Create(object control)
        {
            var logical = control as AvaloniaObject;
            return logical != null ? new[] { new LogicalTreeNode(logical, null) } : Array.Empty<LogicalTreeNode>();
        }

        internal class LogicalTreeNodeCollection : TreeNodeCollection
        {
            private readonly ILogical _control;
            private IDisposable? _subscription;

            public LogicalTreeNodeCollection(TreeNode owner, ILogical control)
                : base(owner)
            {
                _control = control;
            }

            public override void Dispose()
            {
                base.Dispose();
                _subscription?.Dispose();
            }

            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                _subscription = _control.LogicalChildren.ForEachItem(
                    (i, item) => nodes.Insert(i, new LogicalTreeNode((AvaloniaObject)item, Owner)),
                    (i, item) => nodes.RemoveAt(i),
                    () => nodes.Clear());
            }
        }

        internal class TopLevelGroupHostLogical : TreeNodeCollection
        {
            private readonly TopLevelGroup _group;
            private readonly List<IDisposable> _subscriptions = [];

            public TopLevelGroupHostLogical(TreeNode owner, TopLevelGroup host) : base(owner)
            {
                _group = host;
            }

            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                for (var i = 0; i < _group.Items.Count; i++)
                {
                    var window = _group.Items[i];
                    if (window is MainWindow)
                    {
                        continue;
                    }
                    nodes.Add(new LogicalTreeNode(window, Owner));
                }
                void GroupOnAdded(object? sender, TopLevel e)
                {
                    if (e is MainWindow)
                    {
                        return;
                    }

                    nodes.Add(new LogicalTreeNode(e, Owner));
                }
                void GroupOnRemoved(object? sender, TopLevel e)
                {
                    if (e is MainWindow)
                    {
                        return;
                    }

                    nodes.Add(new LogicalTreeNode(e, Owner));
                }
                
                _group.Added += GroupOnAdded;
                _group.Removed += GroupOnRemoved;

                _subscriptions.Add(new AnonymousDisposable(() =>
                {
                    _group.Added -= GroupOnAdded;
                    _group.Removed -= GroupOnRemoved;
                }));
            }

            public override void Dispose()
            {
                foreach (var disposable in _subscriptions) disposable.Dispose();
                _subscriptions.Clear();

                base.Dispose();
            }
        }
    }

    file sealed class AnonymousDisposable(Action dispose) : IDisposable
    {
        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            dispose();
            _isDisposed = true;
        }
    }
}
