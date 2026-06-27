using Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Elements.Trees;
using ClassicDiagnostics.Avalonia.ViewModels;
using ClassicDiagnostics.Avalonia.Elements;
using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;
using ClassicDiagnostics.Avalonia.Rooting;
using ClassicDiagnostics.Avalonia.Shell;

namespace ClassicDiagnostics.Avalonia.Tests.ViewModels;

internal sealed class PinnedPropertyStoreTests
{
    [Test]
    public void StorePinsUnpinsAndTogglesKeys()
    {
        var store = new PinnedPropertyStore();

        Assert.That(store.Contains("Button.Content"), Is.False);
        Assert.That(store.Pin("Button.Content"), Is.True);
        Assert.That(store.Contains("Button.Content"), Is.True);
        Assert.That(store.Pin("Button.Content"), Is.True);
        Assert.That(store.Contains("Button.Content"), Is.True);
        Assert.That(store.Toggle("Button.Content"), Is.False);
        Assert.That(store.Contains("Button.Content"), Is.False);
        Assert.That(store.Toggle("Button.Content"), Is.True);
        Assert.That(store.Contains("Button.Content"), Is.True);
        Assert.That(store.Unpin("Button.Content"), Is.False);
        Assert.That(store.Contains("Button.Content"), Is.False);
        Assert.That(store.Unpin("Button.Content"), Is.False);
    }

    [Test]
    public void TogglePinnedPropertyUpdatesStoreAndPropertyGroup()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var store = new PinnedPropertyStore();
            var details = CreateDetails(new Button(), store, out var main, out var tree);

            try
            {
                var property = FindContentProperty(details);

                details.TogglePinnedProperty(property);

                Assert.That(store.Contains(property.FullName), Is.True);
                Assert.That(property.IsPinned, Is.True);
                Assert.That(property.Group, Is.EqualTo("Pinned"));

                details.TogglePinnedProperty(property);

                Assert.That(store.Contains(property.FullName), Is.False);
                Assert.That(property.IsPinned, Is.False);
                Assert.That(property.Group, Is.Not.EqualTo("Pinned"));
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
    public void SharedStoreMarksPinnedPropertyWhenDetailsAreRecreated()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            var store = new PinnedPropertyStore();
            var firstDetails = CreateDetails(button, store, out var main, out var tree);
            var isFirstDetailsDisposed = false;

            try
            {
                var firstProperty = FindContentProperty(firstDetails);
                firstDetails.TogglePinnedProperty(firstProperty);
                firstDetails.Dispose();
                isFirstDetailsDisposed = true;

                var secondDetails = new ElementDetailsViewModel(tree, button, store);
                try
                {
                    var secondProperty = FindContentProperty(secondDetails);

                    Assert.That(secondProperty.IsPinned, Is.True);
                    Assert.That(secondProperty.Group, Is.EqualTo("Pinned"));
                }
                finally
                {
                    secondDetails.Dispose();
                }
            }
            finally
            {
                if (!isFirstDetailsDisposed)
                {
                    firstDetails.Dispose();
                }

                tree.Dispose();
                main.Dispose();
            }
        });
    }

    private static ElementDetailsViewModel CreateDetails(
        Control target,
        IPinnedPropertyStore store,
        out MainViewModel main,
        out ElementsTreeViewModel tree)
    {
        var root = new StackPanel();
        root.Children.Add(target);
        main = new MainViewModel(root);
        var coordinator = new SelectionCoordinator(store, () => false, _ => { });
        tree = new ElementsTreeViewModel(main, new LogicalTreeProvider().Create(root), coordinator);
        coordinator.Attach(tree, tree);
        return new ElementDetailsViewModel(tree, target, store);
    }

    private static AvaloniaPropertyViewModel FindContentProperty(ElementDetailsViewModel details)
    {
        return details.PropertiesView!
            .OfType<AvaloniaPropertyViewModel>()
            .Single(property => property.Property == ContentControl.ContentProperty);
    }
}
