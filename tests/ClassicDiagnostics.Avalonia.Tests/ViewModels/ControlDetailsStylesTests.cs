using Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Elements;
using ClassicDiagnostics.Avalonia.Elements.Trees;
using ClassicDiagnostics.Avalonia.Shell;
using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;
using ClassicDiagnostics.Avalonia.Rooting;

namespace ClassicDiagnostics.Avalonia.Tests.ViewModels;

internal sealed class ControlDetailsStylesTests
{
    [Test]
    public void AddClassWritesToStyledElementAndRefreshesList()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            var details = CreateDetails(button, out var main, out var tree);

            try
            {
                details.NewClassName = ".primary";
                details.AddClass();

                Assert.That(button.Classes, Does.Contain("primary"));
                Assert.That(details.Classes.Select(x => x.Name), Is.EquivalentTo(new[] { "primary" }));
                Assert.That(details.NewClassName, Is.Empty);
                Assert.That(details.ClassEditError, Is.Null);
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
    public void RemoveClassWritesToStyledElementAndRefreshesList()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            button.Classes.Add("primary");
            var details = CreateDetails(button, out var main, out var tree);

            try
            {
                details.Classes.Single().Remove();

                Assert.That(button.Classes, Does.Not.Contain("primary"));
                Assert.That(details.Classes, Is.Empty);
                Assert.That(details.ClassEditError, Is.Null);
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
    public void AddClassRejectsInvalidInput()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            var details = CreateDetails(button, out var main, out var tree);

            try
            {
                details.NewClassName = string.Empty;
                details.AddClass();
                Assert.That(details.ClassEditError, Is.Not.Null);
                Assert.That(button.Classes, Is.Empty);

                details.NewClassName = ":pointerover";
                details.AddClass();
                Assert.That(details.ClassEditError, Does.Contain("Pseudo classes"));
                Assert.That(button.Classes, Is.Empty);

                details.NewClassName = "primary";
                details.AddClass();
                details.NewClassName = ".primary";
                details.AddClass();
                Assert.That(details.ClassEditError, Does.Contain("already exists"));
                Assert.That(button.Classes.Count(x => x == "primary"), Is.EqualTo(1));
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
    public void AddPseudoClassNormalizesNameAndActivatesIt()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            var details = CreateDetails(button, out var main, out var tree);

            try
            {
                details.NewPseudoClassName = "pointerover";
                details.AddPseudoClass();

                Assert.That(button.Classes, Does.Contain(":pointerover"));
                Assert.That(details.PseudoClasses.Single(x => x.Name == ":pointerover").IsActive, Is.True);
                Assert.That(details.NewPseudoClassName, Is.Empty);
                Assert.That(details.PseudoClassEditError, Is.Null);
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
    public void PseudoClassToggleWritesThroughPseudoClassesApi()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            var details = CreateDetails(button, out var main, out var tree);

            try
            {
                details.NewPseudoClassName = ":pointerover";
                details.AddPseudoClass();
                var pseudoClass = details.PseudoClasses.Single(x => x.Name == ":pointerover");

                pseudoClass.IsActive = false;

                Assert.That(button.Classes, Does.Not.Contain(":pointerover"));
                Assert.That(pseudoClass.Error, Is.Null);
            }
            finally
            {
                details.Dispose();
                tree.Dispose();
                main.Dispose();
            }
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
        var coordinator = new SelectionCoordinator(new PinnedPropertyStore(), () => false, _ => { });
        tree = new ElementsTreeViewModel(main, new LogicalTreeProvider().Create(root), coordinator);
        coordinator.Attach(tree, tree);
        return new ElementDetailsViewModel(tree, target, new PinnedPropertyStore());
    }
}
