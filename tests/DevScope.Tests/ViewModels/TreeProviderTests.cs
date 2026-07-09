using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using RolandUI.DevScope.Models;
using RolandUI.DevScope.Elements.Trees;
using RolandUI.DevScope.ViewModels;
using RolandUI.DevScope.Views.Controls;
using RolandUI.DevScope.Elements;
using RolandUI.DevScope.Elements.Properties.Models;
using RolandUI.DevScope.Elements.Properties.Services;
using RolandUI.DevScope.Elements.Properties.ViewModels;
using RolandUI.DevScope.Rooting;
using RolandUI.DevScope.Shell;

namespace RolandUI.DevScope.Tests.ViewModels;

internal sealed class TreeProviderTests
{
    [Test]
    public void LogicalProviderTracksPresentationRootCollectionChanges()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var firstWindow = new Window();
            var secondWindow = new Window();
            var thirdWindow = new Window();
            var windows = new ObservableCollection<TopLevel>
            {
                firstWindow,
                secondWindow,
            };
            var source = new TestRootSource(windows);
            var host = new PresentationRootNode(source);
            var root = new LogicalTreeProvider().Create(host).Single();

            try
            {
                Assert.That(root.Children, Has.Count.EqualTo(2));
                Assert.That(root.Children[0].Target, Is.SameAs(firstWindow));
                Assert.That(root.Children[1].Target, Is.SameAs(secondWindow));

                windows.Remove(firstWindow);

                Assert.That(root.Children, Has.Count.EqualTo(1));
                Assert.That(root.Children[0].Target, Is.SameAs(secondWindow));

                windows.Add(thirdWindow);

                Assert.That(root.Children, Has.Count.EqualTo(2));
                Assert.That(root.Children[0].Target, Is.SameAs(secondWindow));
                Assert.That(root.Children[1].Target, Is.SameAs(thirdWindow));

                root.Dispose();
                windows.Remove(secondWindow);

                Assert.That(root.Children, Has.Count.EqualTo(2));
            }
            finally
            {
                root.Dispose();
                host.Dispose();
            }
        });
    }

    [Test]
    public void VisualProviderTracksPresentationRootCollectionChanges()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var firstWindow = new Window();
            var secondWindow = new Window();
            var thirdWindow = new Window();
            var windows = new ObservableCollection<TopLevel>
            {
                firstWindow,
                secondWindow,
            };
            var source = new TestRootSource(windows);
            var host = new PresentationRootNode(source);
            var root = new VisualTreeProvider().Create(host).Single();

            try
            {
                Assert.That(root.Children, Has.Count.EqualTo(2));
                Assert.That(root.Children[0].Target, Is.SameAs(firstWindow));
                Assert.That(root.Children[1].Target, Is.SameAs(secondWindow));

                windows.Remove(firstWindow);

                Assert.That(root.Children, Has.Count.EqualTo(1));
                Assert.That(root.Children[0].Target, Is.SameAs(secondWindow));

                windows.Add(thirdWindow);

                Assert.That(root.Children, Has.Count.EqualTo(2));
                Assert.That(root.Children[0].Target, Is.SameAs(secondWindow));
                Assert.That(root.Children[1].Target, Is.SameAs(thirdWindow));

                root.Dispose();
                windows.Remove(secondWindow);

                Assert.That(root.Children, Has.Count.EqualTo(2));
            }
            finally
            {
                root.Dispose();
                host.Dispose();
            }
        });
    }

    [Test]
    public void TreeNodeViewModelTracksClassesUntilDisposed()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            var model = new TreeNodeModel(button, null, _ => TreeNodeModelCollection.Empty);
            var node = new TreeNodeViewModel(model);

            try
            {
                Assert.That(node.Classes, Is.Empty);

                button.Classes.Add("primary");

                Assert.That(node.Classes, Is.EqualTo("(primary)"));

                node.Dispose();
                button.Classes.Add("danger");

                Assert.That(node.Classes, Is.EqualTo("(primary)"));
            }
            finally
            {
                node.Dispose();
            }
        });
    }

    [Test]
    public void SelectionCoordinatorCreatesDetailsAndCanSelectMatchingVisualNode()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var root = new StackPanel();
            var button = new Button();
            root.Children.Add(button);

            var main = new MainViewModel(root);
            var selectedVisualTree = false;
            var coordinator = new SelectionCoordinator(new PinnedPropertyStore(), () => true, value => selectedVisualTree = value);
            var logicalTree = new ElementsTreeViewModel(main, new LogicalTreeProvider().Create(root), coordinator);
            var visualTree = new ElementsTreeViewModel(main, new VisualTreeProvider().Create(root), coordinator);
            coordinator.Attach(logicalTree, visualTree);

            try
            {
                Assert.That(coordinator.SelectControl(button, logicalTree), Is.True);
                Assert.That(logicalTree.SelectedNode?.Model.Target, Is.SameAs(button));
                Assert.That(logicalTree.Details, Is.Not.Null);

                coordinator.RequestTreeNavigateTo(button, true);
                Dispatcher.UIThread.RunJobs();

                Assert.That(selectedVisualTree, Is.True);
                Assert.That(visualTree.SelectedNode?.Model.Target, Is.SameAs(button));
                Assert.That(visualTree.SelectedNode?.Parent?.IsExpanded, Is.True);
            }
            finally
            {
                logicalTree.Dispose();
                visualTree.Dispose();
                main.Dispose();
            }
        });
    }

    private sealed class TestRootSource(ObservableCollection<TopLevel> windows) : IDevToolsRootSource
    {
        public IReadOnlyList<TopLevel> Items => windows;
    }
}
