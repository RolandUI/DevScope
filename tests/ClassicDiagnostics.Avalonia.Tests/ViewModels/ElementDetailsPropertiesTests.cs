using Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Elements;
using ClassicDiagnostics.Avalonia.Elements.Trees;
using ClassicDiagnostics.Avalonia.Properties;
using ClassicDiagnostics.Avalonia.Shell;
using ClassicDiagnostics.Avalonia.Views.Elements.Properties;

namespace ClassicDiagnostics.Avalonia.Tests.ViewModels;

internal sealed class ElementDetailsPropertiesTests
{
    [Test]
    public void ElementDetailsCreatesRootPropertyExplorer()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            var details = CreateDetails(button, out var main, out var tree);

            try
            {
                Assert.That(details.PropertyExplorer.RootObjectColumn, Is.Not.Null);
                Assert.That(details.PropertyExplorer.RootObjectColumn!.Target, Is.SameAs(button));
            }
            finally
            {
                details.Dispose();
                tree.Dispose();
                main.Dispose();
            }
        });
    }

    [Test]
    public void ElementsPageExposesCurrentPropertyExplorerFromSelectedDetails()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var root = new StackPanel();
            var button = new Button();
            root.Children.Add(button);

            var main = new MainViewModel(root);
            var coordinator = new SelectionCoordinator(new PinnedPropertyStore(), () => false, _ => { });
            var logicalTree = new ElementsTreeViewModel(main, new LogicalTreeProvider().Create(root), coordinator);
            var visualTree = new ElementsTreeViewModel(main, new VisualTreeProvider().Create(root), coordinator);
            coordinator.Attach(logicalTree, visualTree);
            var page = new ElementsPageViewModel(logicalTree, visualTree, coordinator);

            try
            {
                Assert.That(page.CurrentPropertyExplorer, Is.Null);

                Assert.That(coordinator.SelectControl(button, logicalTree), Is.True);

                Assert.That(logicalTree.Details, Is.Not.Null);
                Assert.That(page.CurrentDetails, Is.SameAs(logicalTree.Details));
                Assert.That(page.CurrentPropertyExplorer, Is.SameAs(logicalTree.Details!.PropertyExplorer));
                Assert.That(page.CurrentPropertyExplorer!.RootObjectColumn!.Target, Is.SameAs(button));
            }
            finally
            {
                page.Dispose();
                logicalTree.Dispose();
                visualTree.Dispose();
                main.Dispose();
            }
        });
    }

    [Test]
    public void PropertyExplorerViewLoadsXamlContent()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var view = new PropertyExplorerView();

            Assert.That(view.Content, Is.Not.Null);
        });
    }

    private static ElementDetailsViewModel CreateDetails(
        Control target,
        out MainViewModel main,
        out ElementsTreeViewModel tree)
    {
        var root = new StackPanel();
        root.Children.Add(target);
        main = new MainViewModel(root);
        var store = new PinnedPropertyStore();
        var coordinator = new SelectionCoordinator(store, () => false, _ => { });
        tree = new ElementsTreeViewModel(main, new LogicalTreeProvider().Create(root), coordinator);
        coordinator.Attach(tree, tree);
        return new ElementDetailsViewModel(tree, target, store);
    }
}
