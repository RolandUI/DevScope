using Avalonia.Media;
using ClassicDiagnostics.Avalonia.Properties;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Tests.ViewModels;

internal sealed class PropertyEditorFactoryTests
{
    [Test]
    public void FactorySelectsExistingEditorKinds()
    {
        AssertKind(typeof(bool), PropertyEditorKind.Boolean);
        AssertKind(typeof(int), PropertyEditorKind.Numeric);
        AssertKind(typeof(int?), PropertyEditorKind.Numeric);
        AssertKind(typeof(float), PropertyEditorKind.Numeric);
        AssertKind(typeof(double), PropertyEditorKind.Text);
        AssertKind(typeof(decimal), PropertyEditorKind.Text);
        AssertKind(typeof(TestEnum), PropertyEditorKind.Enum);
        AssertKind(typeof(TestFlags), PropertyEditorKind.FlagsEnum);
        AssertKind(typeof(Color), PropertyEditorKind.Color);
        AssertKind(typeof(IBrush), PropertyEditorKind.Brush);
        AssertKind(typeof(SolidColorBrush), PropertyEditorKind.Brush);
        AssertKind(typeof(IImage), PropertyEditorKind.Image);
        AssertKind(typeof(Geometry), PropertyEditorKind.Geometry);
        AssertKind(typeof(string), PropertyEditorKind.Text);
        AssertKind(typeof(DateTime), PropertyEditorKind.Text);
        AssertKind(typeof(object), PropertyEditorKind.ReadOnlyText);
        AssertKind(typeof(ComplexTarget), PropertyEditorKind.ReadOnlyText);
        AssertKind(typeof(ComplexTarget), PropertyEditorKind.ComplexObject, new ComplexTarget());
    }

    [Test]
    public void FactoryMarksEditableAndNavigableState()
    {
        var editableText = CreateDescriptor(typeof(string));
        var readonlyText = CreateDescriptor(typeof(string), isReadOnly: true);
        var readonlyBoolean = CreateDescriptor(typeof(bool), isReadOnly: true);
        var readonlyFlags = CreateDescriptor(typeof(TestFlags), isReadOnly: true);
        var nullComplex = CreateDescriptor(typeof(ComplexTarget));
        var complex = CreateDescriptor(typeof(ComplexTarget), value: new ComplexTarget());

        Assert.That(editableText.CanEdit, Is.True);
        Assert.That(editableText.CanNavigate, Is.False);
        Assert.That(readonlyText.Kind, Is.EqualTo(PropertyEditorKind.ReadOnlyText));
        Assert.That(readonlyText.CanEdit, Is.False);
        Assert.That(readonlyBoolean.Kind, Is.EqualTo(PropertyEditorKind.Boolean));
        Assert.That(readonlyBoolean.CanEdit, Is.False);
        Assert.That(readonlyFlags.Kind, Is.EqualTo(PropertyEditorKind.FlagsEnum));
        Assert.That(readonlyFlags.CanEdit, Is.False);
        Assert.That(nullComplex.CanNavigate, Is.False);
        Assert.That(complex.CanEdit, Is.False);
        Assert.That(complex.CanNavigate, Is.True);
    }

    private static void AssertKind(Type propertyType, PropertyEditorKind expectedKind, object? value = null)
    {
        Assert.That(CreateDescriptor(propertyType, value: value).Kind, Is.EqualTo(expectedKind));
    }

    private static PropertyEditorDescriptor CreateDescriptor(
        Type propertyType,
        bool isReadOnly = false,
        object? value = null)
    {
        return PropertyEditorFactory.Default.Create(new TestPropertyViewModel(propertyType, isReadOnly, value));
    }

    private enum TestEnum
    {
        First,
        Second,
    }

    [Flags]
    private enum TestFlags
    {
        None = 0,
        Read = 1,
        Write = 2,
    }

    private sealed class ComplexTarget
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestPropertyViewModel(
        Type propertyType,
        bool isReadOnly,
        object? value) : PropertyViewModel
    {
        public override Type AssignedType => propertyType;

        public override Type? DeclaringType => typeof(TestPropertyViewModel);

        public override string Group => "Properties";

        public override bool? IsAttached => false;

        public override bool IsReadonly => isReadOnly;

        public override object Key => Name;

        public override string Name => propertyType.Name;

        public override string Priority => string.Empty;

        public override Type PropertyType => propertyType;

        public override object? Value { get; set; } = value;

        public override void Update()
        {
        }
    }
}
