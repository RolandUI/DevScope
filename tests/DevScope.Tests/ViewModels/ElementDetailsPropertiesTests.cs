using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RolandUI.DevScope.Elements;
using RolandUI.DevScope.Elements.Trees;
using RolandUI.DevScope.Shell;
using RolandUI.DevScope.Views.Elements;
using RolandUI.DevScope.Views.Elements.Properties;
using RolandUI.DevScope.Views.Elements.Styles;

namespace RolandUI.DevScope.Tests.ViewModels;

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
    public void ElementsPageDoesNotLogBindingErrorsWhenDetailsAreSelected()
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
            var previousSink = Logger.Sink;
            var sink = new BindingLogSink();
            Window? window = null;

            try
            {
                Assert.That(coordinator.SelectControl(button, logicalTree), Is.True);
                Logger.Sink = sink;

                var view = new ElementsPage(page);
                window = new Window
                {
                    Width = 900,
                    Height = 600,
                    Content = view,
                };
                window.Show();
                Dispatcher.UIThread.RunJobs();

                var tabControl = view.GetVisualDescendants().OfType<TabControl>().Single();
                var stylesView = view.GetVisualDescendants().OfType<ElementStylesView>().Single();
                tabControl.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                var propertyExplorerView = view.GetVisualDescendants().OfType<PropertyExplorerView>().Single();

                Assert.Multiple(() =>
                {
                    Assert.That(stylesView.DataContext, Is.SameAs(page.CurrentDetails));
                    Assert.That(propertyExplorerView.DataContext, Is.SameAs(page.CurrentPropertyExplorer));
                    Assert.That(
                        sink.Entries,
                        Is.Empty,
                        () => string.Join(Environment.NewLine, sink.Entries));
                });
            }
            finally
            {
                window?.Close();
                Logger.Sink = previousSink;
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

    private sealed class BindingLogSink : ILogSink
    {
        public List<string> Entries { get; } = [];

        public bool IsEnabled(LogEventLevel level, string area)
        {
            return area == LogArea.Binding;
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        {
            Record(level, area, messageTemplate, []);
        }

        public void Log(
            LogEventLevel level,
            string area,
            object? source,
            string messageTemplate,
            params object?[] propertyValues)
        {
            Record(level, area, messageTemplate, propertyValues);
        }

        private void Record(
            LogEventLevel level,
            string area,
            string messageTemplate,
            IReadOnlyList<object?> propertyValues)
        {
            if (area != LogArea.Binding)
            {
                return;
            }

            Entries.Add($"{level}: {messageTemplate} {string.Join(" | ", propertyValues)}".TrimEnd());
        }
    }
}
