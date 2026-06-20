using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Models;

namespace ClassicDiagnostics.Avalonia.Tests.ViewModels;

internal sealed class LogicalTreeNodeTests
{
    [Test]
    public void PresentationRootGroupLogicalChildrenTrackRootCollectionChanges()
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
            var host = new PresentationRootGroup(source);
            var root = LogicalTreeNode.Create(host).Single();

            try
            {
                Assert.That(root.Children, Has.Count.EqualTo(2));
                Assert.That(root.Children[0].Visual, Is.SameAs(firstWindow));
                Assert.That(root.Children[1].Visual, Is.SameAs(secondWindow));

                windows.Remove(firstWindow);

                Assert.That(root.Children, Has.Count.EqualTo(1));
                Assert.That(root.Children[0].Visual, Is.SameAs(secondWindow));

                windows.Add(thirdWindow);

                Assert.That(root.Children, Has.Count.EqualTo(2));
                Assert.That(root.Children[0].Visual, Is.SameAs(secondWindow));
                Assert.That(root.Children[1].Visual, Is.SameAs(thirdWindow));

                root.Dispose();
                windows.Remove(secondWindow);

                // Once the logical tree is disposed, the host may still raise collection
                // changes, but this node collection must no longer mutate.
                Assert.That(root.Children, Has.Count.EqualTo(2));
            }
            finally
            {
                root.Dispose();
                host.Dispose();
            }
        });
    }

    private sealed class TestRootSource(ObservableCollection<TopLevel> windows) : IDevToolsRootSource
    {
        public IReadOnlyList<TopLevel> Items => windows;
    }
}
