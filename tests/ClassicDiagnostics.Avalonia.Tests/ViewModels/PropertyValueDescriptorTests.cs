using ClassicDiagnostics.Avalonia.Elements;
using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;
using ClassicDiagnostics.Avalonia.Elements.Trees;
using ClassicDiagnostics.Avalonia.Rooting;
using ClassicDiagnostics.Avalonia.Shell;

namespace ClassicDiagnostics.Avalonia.Tests.ViewModels;

internal sealed class PropertyValueDescriptorTests
{
    [Test]
    public void FactoryClassifiesValues()
    {
        AssertKind(null, PropertyValueDescriptorKind.Null, canNavigate: false);
        AssertKind("text", PropertyValueDescriptorKind.Simple, canNavigate: false);
        AssertKind(42, PropertyValueDescriptorKind.Simple, canNavigate: false);
        AssertKind(new ComplexTarget(), PropertyValueDescriptorKind.Object, canNavigate: true);
        AssertKind(new[] { 1, 2 }, PropertyValueDescriptorKind.Array, canNavigate: true);
        AssertKind(new List<string> { "a", "b" }, PropertyValueDescriptorKind.List, canNavigate: true);
        AssertKind(new Dictionary<string, int> { ["one"] = 1 }, PropertyValueDescriptorKind.Dictionary, canNavigate: true);
        AssertKind(Enumerable.Range(0, 2).Where(x => x >= 0), PropertyValueDescriptorKind.Enumerable, canNavigate: true);
    }

    [Test]
    public void FactoryDiscoversArrayAndListChildren()
    {
        var array = PropertyValueDescriptorFactory.Create(new[] { "first", "second" });
        var list = PropertyValueDescriptorFactory.Create(new List<int> { 10, 20 });

        Assert.That(array.Children.Select(child => child.Name), Is.EqualTo(new[] { "[0]", "[1]" }));
        Assert.That(array.Children.Select(child => child.Value), Is.EqualTo(new[] { "first", "second" }));
        Assert.That(list.Children.Select(child => child.Name), Is.EqualTo(new[] { "[0]", "[1]" }));
        Assert.That(list.Children.Select(child => child.Value), Is.EqualTo(new[] { 10, 20 }));
    }

    [Test]
    public void FactoryDiscoversDictionaryChildren()
    {
        var descriptor = PropertyValueDescriptorFactory.Create(
            new Dictionary<string, object?>
            {
                ["name"] = "Avalonia",
                ["child"] = new ComplexTarget(),
            });

        Assert.That(descriptor.Kind, Is.EqualTo(PropertyValueDescriptorKind.Dictionary));
        Assert.That(descriptor.Children.Select(child => child.Name), Is.EqualTo(new[] { "[name]", "[child]" }));
        Assert.That(descriptor.Children[0].Value, Is.EqualTo("Avalonia"));
        Assert.That(descriptor.Children[1].CanNavigate, Is.True);
    }

    private static void AssertKind(object? value, PropertyValueDescriptorKind kind, bool canNavigate)
    {
        var descriptor = PropertyValueDescriptorFactory.Create(value);

        Assert.That(descriptor.Kind, Is.EqualTo(kind));
        Assert.That(descriptor.CanNavigate, Is.EqualTo(canNavigate));
    }

    private sealed class ComplexTarget
    {
        public string Name { get; set; } = string.Empty;
    }
}
