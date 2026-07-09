using System.Collections;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using RolandUI.DevScope.Elements.Properties;
using RolandUI.DevScope.Elements.Properties.Services;
using RolandUI.DevScope.Elements.Properties.ViewModels;
using RolandUI.DevScope.Views.Elements.Properties;

namespace RolandUI.DevScope.Tests.ViewModels;

internal sealed class CollectionItemEditingTests
{
    [Test]
    public void ArrayItemWritesBackAndRefreshesRow()
    {
        var values = new[] { 1 };
        using var column = CreateColumn(values);
        var item = column.Items.Single();
        var changedProperties = new List<string?>();
        item.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        item.Value = 2;

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Is.EqualTo(2));
            Assert.That(item.Value, Is.EqualTo(2));
            Assert.That(item.ValueText, Is.EqualTo("2"));
            Assert.That(item.ValueError, Is.Null);
            Assert.That(item.IsReadonly, Is.False);
            Assert.That(PropertyEditorFactory.Default.Create(item).CanEdit, Is.True);
            Assert.That(changedProperties, Does.Contain(nameof(PropertyContainerItemViewModel.Value)));
            Assert.That(changedProperties, Does.Contain(nameof(PropertyContainerItemViewModel.ValueText)));
        });
    }

    [Test]
    public void FixedSizeListAllowsReplacingExistingItem()
    {
        var values = ArrayList.FixedSize(new ArrayList { "before" });
        using var column = CreateColumn(values);
        var item = column.Items.Single();

        item.Value = "after";

        Assert.Multiple(() =>
        {
            Assert.That(values.IsFixedSize, Is.True);
            Assert.That(values.IsReadOnly, Is.False);
            Assert.That(values[0], Is.EqualTo("after"));
            Assert.That(item.IsReadonly, Is.False);
        });
    }

    [Test]
    public void ReadOnlyListDisablesEditorAndRejectsWrites()
    {
        var values = ArrayList.ReadOnly(new ArrayList { "before" });
        using var column = CreateColumn(values);
        var item = column.Items.Single();

        item.Value = "after";

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Is.EqualTo("before"));
            Assert.That(item.IsReadonly, Is.True);
            Assert.That(item.ValueError, Does.Contain("read-only"));
            Assert.That(PropertyEditorFactory.Default.Create(item).CanEdit, Is.False);
        });
    }

    [Test]
    public void DictionaryItemWritesBackByKey()
    {
        var values = new Dictionary<string, int> { ["answer"] = 41 };
        using var column = CreateColumn(values);
        var item = column.Items.Single();

        item.Value = 42;

        Assert.Multiple(() =>
        {
            Assert.That(values["answer"], Is.EqualTo(42));
            Assert.That(item.KeyText, Is.EqualTo("answer"));
            Assert.That(item.Value, Is.EqualTo(42));
            Assert.That(item.ValueError, Is.Null);
        });
    }

    [Test]
    public void ReadOnlyDictionaryDisablesEditorAndRejectsWrites()
    {
        IDictionary values = new ReadOnlyDictionary<string, int>(
            new Dictionary<string, int> { ["answer"] = 41 });
        using var column = CreateColumn(values);
        var item = column.Items.Single();

        item.Value = 42;

        Assert.Multiple(() =>
        {
            Assert.That(values["answer"], Is.EqualTo(41));
            Assert.That(item.IsReadonly, Is.True);
            Assert.That(item.ValueError, Does.Contain("read-only"));
            Assert.That(PropertyEditorFactory.Default.Create(item).CanEdit, Is.False);
        });
    }

    [Test]
    public void NullItemsUseDeclaredCollectionValueType()
    {
        string?[] array = [null];
        var dictionary = new Dictionary<string, string?> { ["name"] = null };
        using var arrayColumn = CreateColumn(array);
        using var dictionaryColumn = CreateColumn(dictionary);
        var arrayItem = arrayColumn.Items.Single();
        var dictionaryItem = dictionaryColumn.Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(arrayItem.PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(dictionaryItem.PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(PropertyEditorFactory.Default.Create(arrayItem).Kind, Is.EqualTo(PropertyEditorKind.Text));
            Assert.That(PropertyEditorFactory.Default.Create(dictionaryItem).Kind, Is.EqualTo(PropertyEditorKind.Text));
        });

        arrayItem.Value = "array";
        dictionaryItem.Value = "dictionary";

        Assert.Multiple(() =>
        {
            Assert.That(array[0], Is.EqualTo("array"));
            Assert.That(dictionary["name"], Is.EqualTo("dictionary"));
        });
    }

    [Test]
    public void InvalidWriteIsSurfacedAndSourceValueIsRestored()
    {
        AvaloniaTestFixture.RunOnUIThread(() =>
        {
            var values = new List<int> { 10 };
            using var column = CreateColumn(values);
            var item = column.Items.Single();
            var editor = new PropertyValueEditorView { DataContext = item };
            var editorControl = editor.Content as Control;

            item.Value = "not an integer";

            Assert.Multiple(() =>
            {
                Assert.That(values[0], Is.EqualTo(10));
                Assert.That(item.Value, Is.EqualTo(10));
                Assert.That(item.ValueError, Does.Contain("cannot be assigned"));
                Assert.That(editorControl, Is.Not.Null);
                Assert.That(DataValidationErrors.GetHasErrors(editorControl!), Is.True);
            });

            editor.DataContext = null;
        });
    }

    [Test]
    public void PlainEnumerableItemsRemainInspectOnly()
    {
        var values = Enumerable.Range(1, 1).Where(value => value > 0);
        using var column = CreateColumn(values);

        column.ShowMore();
        var item = column.Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(item.IsReadonly, Is.True);
            Assert.That(PropertyEditorFactory.Default.Create(item).CanEdit, Is.False);
        });
    }

    [Test]
    public void LegacySyntheticChildUsesSameMutableAccessor()
    {
        var values = new List<int> { 1 };
        var inspection = PropertyInspector.Default.Inspect(values, PropertyInspectionOptions.Default);
        var item = inspection.Properties.OfType<PropertyValueChildViewModel>().Single();

        item.Value = 2;

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Is.EqualTo(2));
            Assert.That(item.Value, Is.EqualTo(2));
            Assert.That(item.IsReadonly, Is.False);
        });
    }

    private static ContainerPropertiesColumnViewModel CreateColumn(object target)
    {
        var kind = PropertyColumnNavigation.GetColumnKind(target)
            ?? throw new InvalidOperationException("Target is not a supported container.");

        return new ContainerPropertiesColumnViewModel(target, "Items", "Items", kind);
    }
}
