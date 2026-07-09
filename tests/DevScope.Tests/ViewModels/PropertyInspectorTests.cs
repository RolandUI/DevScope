using Avalonia;
using Avalonia.Controls;
using RolandUI.DevScope.ViewModels;
using RolandUI.DevScope.Elements;
using RolandUI.DevScope.Elements.Properties.Models;
using RolandUI.DevScope.Elements.Properties.Services;
using RolandUI.DevScope.Elements.Properties.ViewModels;
using RolandUI.DevScope.Elements.Trees;
using RolandUI.DevScope.Rooting;
using RolandUI.DevScope.Shell;

namespace RolandUI.DevScope.Tests.ViewModels;

internal sealed class PropertyInspectorTests
{
    [Test]
    public void InspectorDiscoversAvaloniaAndAttachedProperties()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var result = PropertyInspector.Default.Inspect(new Button(), PropertyInspectionOptions.Default);

            var avaloniaProperties = result.Properties.OfType<AvaloniaPropertyViewModel>().ToArray();

            Assert.That(avaloniaProperties.Any(property => property.Property == Button.ContentProperty), Is.True);
            Assert.That(avaloniaProperties.Any(property => property.IsAttached == true), Is.True);
        });
    }

    [Test]
    public void InspectorDiscoversClrPropertiesAndExcludesIndexers()
    {
        var result = PropertyInspector.Default.Inspect(new InspectorTarget(), PropertyInspectionOptions.Default);

        var clrProperties = result.Properties.OfType<ClrPropertyViewModel>().ToArray();

        Assert.That(clrProperties.Any(property => property.Name == nameof(InspectorTarget.Name)), Is.True);
        Assert.That(clrProperties.Any(property => property.Name == "Item"), Is.False);
    }

    [Test]
    public void InspectorOnlyDiscoversInterfacePropertiesWhenEnabled()
    {
        var withoutInterfaces = PropertyInspector.Default.Inspect(
            new InspectorTarget(),
            new PropertyInspectionOptions(false, new PinnedPropertyStore()));
        var withInterfaces = PropertyInspector.Default.Inspect(
            new InspectorTarget(),
            new PropertyInspectionOptions(true, new PinnedPropertyStore()));

        Assert.That(withoutInterfaces.Properties.Any(property => property.Name == "IInspectorContract.ContractName"), Is.False);
        Assert.That(withInterfaces.Properties.Any(property => property.Name == "IInspectorContract.ContractName"), Is.True);
    }

    [Test]
    public void InspectorMarksPinnedProperties()
    {
        var target = new InspectorTarget();
        var pinnedProperty = new ClrPropertyViewModel(
            target,
            typeof(InspectorTarget).GetProperty(nameof(InspectorTarget.Name))!);
        var pinnedProperties = new PinnedPropertyStore();
        pinnedProperties.Pin(pinnedProperty.FullName);

        var result = PropertyInspector.Default.Inspect(
            target,
            new PropertyInspectionOptions(false, pinnedProperties));

        var property = result.Properties.Single(property => property.Name == nameof(InspectorTarget.Name));

        Assert.That(property.IsPinned, Is.True);
        Assert.That(property.Group, Is.EqualTo("Pinned"));
    }

    [Test]
    public void InspectorBuildsPropertyIndexForAvaloniaAndClrChanges()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            var result = PropertyInspector.Default.Inspect(button, PropertyInspectionOptions.Default);

            Assert.That(result.PropertyIndex.TryGetValue(Button.ContentProperty, out var avaloniaProperties), Is.True);
            Assert.That(avaloniaProperties, Has.Some.TypeOf<AvaloniaPropertyViewModel>());
            Assert.That(result.PropertyIndex.TryGetValue(nameof(Button.Content), out var clrProperties), Is.True);
            Assert.That(clrProperties, Has.Some.TypeOf<ClrPropertyViewModel>());
        });
    }

    [Test]
    public void InspectorReturnsSyntheticChildrenForLists()
    {
        var result = PropertyInspector.Default.Inspect(
            new List<object?> { "first", new InspectorTarget() },
            PropertyInspectionOptions.Default);

        var properties = result.Properties.OfType<PropertyValueChildViewModel>().ToArray();

        Assert.That(properties.Select(property => property.Name), Is.EqualTo(new[] { "[0]", "[1]" }));
        Assert.That(properties.Select(property => property.Group), Is.EqualTo(new[] { "Items", "Items" }));
        Assert.That(properties[0].Value, Is.EqualTo("first"));
        Assert.That(PropertyValueDescriptorFactory.Create(properties[1].Value).CanNavigate, Is.True);
    }

    [Test]
    public void InspectorReturnsSyntheticChildrenForDictionaries()
    {
        var result = PropertyInspector.Default.Inspect(
            new Dictionary<string, object?> { ["name"] = "Avalonia" },
            PropertyInspectionOptions.Default);

        var property = result.Properties.OfType<PropertyValueChildViewModel>().Single();

        Assert.That(property.Name, Is.EqualTo("[name]"));
        Assert.That(property.Group, Is.EqualTo("Entries"));
        Assert.That(property.Value, Is.EqualTo("Avalonia"));
    }

    private interface IInspectorContract
    {
        string ContractName { get; }
    }

    private sealed class InspectorTarget : IInspectorContract
    {
        public string ContractName => "Contract";

        public string Name { get; set; } = "Initial";

        public string this[int index] => index.ToString();
    }
}
