using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using RolandUI.DevScope.Elements;
using RolandUI.DevScope.Elements.Properties.Models;
using RolandUI.DevScope.Elements.Properties.Services;
using RolandUI.DevScope.Elements.Properties.ViewModels;
using RolandUI.DevScope.Elements.Trees;
using RolandUI.DevScope.Rooting;
using RolandUI.DevScope.Shell;

namespace RolandUI.DevScope.Tests.ViewModels;

internal sealed class PropertyAccessorTests
{
    [Test]
    public void AvaloniaAccessorReadsAndWritesStyledProperty()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            var accessor = new AvaloniaPropertyAccessor(button, Button.ContentProperty);

            var result = accessor.Write("Hello");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.WrittenValue, Is.EqualTo("Hello"));
            Assert.That(button.Content, Is.EqualTo("Hello"));
            Assert.That(accessor.Value, Is.EqualTo("Hello"));
            Assert.That(accessor.Name, Is.EqualTo("Content"));
            Assert.That(accessor.Group, Is.EqualTo("Properties"));
        });
    }

    [Test]
    public void AvaloniaAccessorReportsReadonlyPropertyWriteFailure()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var button = new Button();
            var accessor = new AvaloniaPropertyAccessor(button, Visual.BoundsProperty);

            var result = accessor.Write(new Rect(0, 0, 10, 10));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Exception, Is.Not.Null);
            Assert.That(result.ErrorMessage, Does.Contain("Bounds"));
        });
    }

    [Test]
    public void ClrAccessorReadsAndWritesSettableProperty()
    {
        var target = new TestClrTarget();
        var accessor = new ClrPropertyAccessor(
            target,
            typeof(TestClrTarget).GetProperty(nameof(TestClrTarget.Name))!);

        var result = accessor.Write("Updated");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(target.Name, Is.EqualTo("Updated"));
        Assert.That(accessor.Value, Is.EqualTo("Updated"));
        Assert.That(accessor.Group, Is.EqualTo("CLR Properties"));
    }

    [Test]
    public void ClrAccessorReportsReadonlyPropertyWriteFailure()
    {
        var target = new TestClrTarget();
        var accessor = new ClrPropertyAccessor(
            target,
            typeof(TestClrTarget).GetProperty(nameof(TestClrTarget.ReadOnlyName))!);

        var result = accessor.Write("Updated");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Exception, Is.Not.Null);
        Assert.That(result.ErrorMessage, Does.Contain(nameof(TestClrTarget.ReadOnlyName)));
    }

    [Test]
    public void PropertyStringConversionSupportsCommonTypes()
    {
        Assert.That(PropertyStringConversion.FromString("42", typeof(int)), Is.EqualTo(42));
        Assert.That(PropertyStringConversion.FromString("42", typeof(int?)), Is.EqualTo(42));
        Assert.That(PropertyStringConversion.FromString("Stretch", typeof(HorizontalAlignment)), Is.EqualTo(HorizontalAlignment.Stretch));
        Assert.That(PropertyStringConversion.FromString("1,2,3,4", typeof(Thickness)), Is.EqualTo(new Thickness(1, 2, 3, 4)));
        Assert.That(PropertyStringConversion.FromString("10,20", typeof(Size)), Is.EqualTo(new Size(10, 20)));
    }

    [Test]
    public void PropertyStringConversionThrowsForInvalidInput()
    {
        Assert.That(
            () => PropertyStringConversion.FromString("not-a-number", typeof(int)),
            Throws.Exception);
    }

    [Test]
    public void PropertyNumericConversionSupportsNullableTargets()
    {
        Assert.That(PropertyNumericConversion.FromDecimal(12m, typeof(int?)), Is.EqualTo(12));
    }

    private sealed class TestClrTarget
    {
        public string Name { get; set; } = "Initial";

        public string ReadOnlyName => "Initial";
    }
}
