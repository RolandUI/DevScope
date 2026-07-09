using Avalonia.Controls;
using RolandUI.DevScope.Elements;
using RolandUI.DevScope.Elements.Search;
using RolandUI.DevScope.Elements.Trees;
using RolandUI.DevScope.Shell;

namespace RolandUI.DevScope.Tests.ViewModels;

internal sealed class ElementsSearchTests
{
    [Test]
    public void SearchMatchesVisibleNodeText()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button
            {
                Name = "SaveButton",
            };
            button.Classes.Add("primary");
            var tree = CreateTree(button, out var main, out var coordinator);

            try
            {
                Assert.That(TreeSearchService.Search(tree, "Button").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
                    node => ReferenceEquals(node.Model.Target, button)));
                Assert.That(TreeSearchService.Search(tree, "SaveButton").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
                    node => ReferenceEquals(node.Model.Target, button)));
                Assert.That(TreeSearchService.Search(tree, "primary").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
                    node => ReferenceEquals(node.Model.Target, button)));
            }
            finally
            {
                tree.Dispose();
                main.Dispose();
            }
        });
    }

    [Test]
    public void SearchMatchesSimpleSelectorParts()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button
            {
                Name = "SaveButton",
            };
            button.Classes.Add("primary");
            ((IPseudoClasses)button.Classes).Set(":pointerover", true);
            var tree = CreateTree(button, out var main, out var coordinator);

            try
            {
                Assert.That(TreeSearchService.Search(tree, "#SaveButton").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
                    node => ReferenceEquals(node.Model.Target, button)));
                Assert.That(TreeSearchService.Search(tree, ".primary").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
                    node => ReferenceEquals(node.Model.Target, button)));
                Assert.That(TreeSearchService.Search(tree, "Button.primary:pointerover").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
                    node => ReferenceEquals(node.Model.Target, button)));
            }
            finally
            {
                tree.Dispose();
                main.Dispose();
            }
        });
    }

    [Test]
    public void SearchDefersXPathLikeQueries()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var tree = CreateTree(new Button(), out var main, out var coordinator);

            try
            {
                var results = TreeSearchService.Search(tree, "//Button");

                Assert.That(results.Matches, Is.Empty);
                Assert.That(results.DeferredMessage, Does.Contain("planned"));
            }
            finally
            {
                tree.Dispose();
                main.Dispose();
            }
        });
    }

    [Test]
    public void SearchHonorsTextFilterOptions()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button
            {
                Name = "SaveButton",
            };
            var tree = CreateTree(button, out var main, out var coordinator);

            try
            {
                Assert.That(
                    TreeSearchService.Search(tree, "savebutton", new TreeSearchOptions(true, false, false)).Matches,
                    Is.Empty);
                Assert.That(
                    TreeSearchService.Search(tree, "Save.*", new TreeSearchOptions(false, true, false)).Matches,
                    Has.Exactly(1).Matches<TreeNodeViewModel>(node => ReferenceEquals(node.Model.Target, button)));
                Assert.That(
                    TreeSearchService.Search(tree, "Save", new TreeSearchOptions(false, false, true)).Matches,
                    Is.Empty);
            }
            finally
            {
                tree.Dispose();
                main.Dispose();
            }
        });
    }

    private static ElementsTreeViewModel CreateTree(
        Control child,
        out MainViewModel main,
        out SelectionCoordinator coordinator)
    {
        var root = new StackPanel();
        root.Children.Add(child);

        main = new MainViewModel(root);
        coordinator = new SelectionCoordinator(new PinnedPropertyStore(), () => false, _ => { });
        var tree = new ElementsTreeViewModel(main, new LogicalTreeProvider().Create(root), coordinator);
        coordinator.Attach(tree, tree);
        return tree;
    }
}
