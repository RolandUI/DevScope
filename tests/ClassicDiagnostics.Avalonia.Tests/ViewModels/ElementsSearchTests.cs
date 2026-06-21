using Avalonia.Controls;
using Avalonia.Styling;
using ClassicDiagnostics.Avalonia.Elements.Search;
using ClassicDiagnostics.Avalonia.Models;
using ClassicDiagnostics.Avalonia.Properties;
using ClassicDiagnostics.Avalonia.Tree;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Tests.ViewModels;

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
                var service = new TreeSearchService();

                Assert.That(service.Search(tree, "Button").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
                    node => ReferenceEquals(node.Model.Target, button)));
                Assert.That(service.Search(tree, "SaveButton").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
                    node => ReferenceEquals(node.Model.Target, button)));
                Assert.That(service.Search(tree, "primary").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
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
                var service = new TreeSearchService();

                Assert.That(service.Search(tree, "#SaveButton").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
                    node => ReferenceEquals(node.Model.Target, button)));
                Assert.That(service.Search(tree, ".primary").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
                    node => ReferenceEquals(node.Model.Target, button)));
                Assert.That(service.Search(tree, "Button.primary:pointerover").Matches, Has.Exactly(1).Matches<TreeNodeViewModel>(
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
                var results = new TreeSearchService().Search(tree, "//Button");

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
                var service = new TreeSearchService();

                Assert.That(
                    service.Search(tree, "savebutton", new TreeSearchOptions(true, false, false)).Matches,
                    Is.Empty);
                Assert.That(
                    service.Search(tree, "Save.*", new TreeSearchOptions(false, true, false)).Matches,
                    Has.Exactly(1).Matches<TreeNodeViewModel>(node => ReferenceEquals(node.Model.Target, button)));
                Assert.That(
                    service.Search(tree, "Save", new TreeSearchOptions(false, false, true)).Matches,
                    Is.Empty);
            }
            finally
            {
                tree.Dispose();
                main.Dispose();
            }
        });
    }

    private static TreePageViewModel CreateTree(
        Control child,
        out MainViewModel main,
        out SelectionCoordinator coordinator)
    {
        var root = new StackPanel();
        root.Children.Add(child);

        main = new MainViewModel(root);
        coordinator = new SelectionCoordinator(new PinnedPropertyStore(), () => false, _ => { });
        var tree = new TreePageViewModel(main, new LogicalTreeProvider().Create(root), coordinator);
        coordinator.Attach(tree, tree);
        return tree;
    }
}
