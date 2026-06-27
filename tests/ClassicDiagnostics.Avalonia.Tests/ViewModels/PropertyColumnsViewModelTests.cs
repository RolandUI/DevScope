using System.Collections;
using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;
using ClassicDiagnostics.Avalonia.Properties;

namespace ClassicDiagnostics.Avalonia.Tests.ViewModels;

internal sealed class PropertyColumnsViewModelTests
{
    [Test]
    public void ColumnsModeAppendsComplexObjectColumn()
    {
        using var columns = CreateColumns(new ColumnRootTarget());
        var rootColumn = columns.RootObjectColumn!;

        rootColumn.SelectedProperty = FindProperty(rootColumn, nameof(ColumnRootTarget.Child));

        Assert.That(columns.Columns, Has.Count.EqualTo(2));
        Assert.That(columns.Columns[1].Title, Is.EqualTo(nameof(ColumnRootTarget.Child)));
        Assert.That(columns.Columns[1].Content.Target, Is.TypeOf<ColumnChildTarget>());
    }

    [Test]
    public void ColumnsModeTrimsStaleColumnsWhenEarlierSelectionChanges()
    {
        using var columns = CreateColumns(new ColumnRootTarget());
        var rootColumn = columns.RootObjectColumn!;

        rootColumn.SelectedProperty = FindProperty(rootColumn, nameof(ColumnRootTarget.Child));
        rootColumn.SelectedProperty = FindProperty(rootColumn, nameof(ColumnRootTarget.Other));

        Assert.That(columns.Columns, Has.Count.EqualTo(2));
        Assert.That(columns.Columns[1].Title, Is.EqualTo(nameof(ColumnRootTarget.Other)));
    }

    [Test]
    public void ColumnsModeSelectingSimpleValueDoesNotAppendColumn()
    {
        using var columns = CreateColumns(new ColumnRootTarget());
        var rootColumn = columns.RootObjectColumn!;

        rootColumn.SelectedProperty = FindProperty(rootColumn, nameof(ColumnRootTarget.Child));
        rootColumn.SelectedProperty = FindProperty(rootColumn, nameof(ColumnRootTarget.Name));

        Assert.That(columns.Columns, Has.Count.EqualTo(1));
    }

    [Test]
    public void ColumnsModeSupportsContainerColumns()
    {
        using var columns = CreateColumns(new ColumnRootTarget());
        var rootColumn = columns.RootObjectColumn!;

        rootColumn.SelectedProperty = FindProperty(rootColumn, nameof(ColumnRootTarget.Children));

        Assert.That(columns.Columns, Has.Count.EqualTo(2));
        Assert.That(columns.Columns[1].Content, Is.TypeOf<ContainerPropertiesColumnViewModel>());
        var containerColumn = (ContainerPropertiesColumnViewModel)columns.Columns[1].Content;
        Assert.That(containerColumn.Items.Select(item => item.Value), Is.EqualTo(new object[] { "first", "second" }));
    }

    [Test]
    public void ColumnsModeNavigatesFromDictionaryItemToObjectColumn()
    {
        using var columns = CreateColumns(new ColumnRootTarget());
        var rootColumn = columns.RootObjectColumn!;

        rootColumn.SelectedProperty = FindProperty(rootColumn, nameof(ColumnRootTarget.Lookup));
        var dictionaryColumn = (ContainerPropertiesColumnViewModel)columns.Columns[1].Content;
        dictionaryColumn.SelectedItem = dictionaryColumn.Items.Single(item => item.KeyText == "child");

        Assert.That(columns.Columns, Has.Count.EqualTo(3));
        Assert.That(columns.Columns[2].Content, Is.TypeOf<ObjectPropertiesColumnViewModel>());
    }

    [Test]
    public void ColumnsModeKeepsFixedResizableWidthForSingleColumn()
    {
        using var columns = CreateColumns(new ColumnRootTarget());
        var rootColumn = columns.Columns[0];

        Assert.That(rootColumn.Width, Is.EqualTo(360));
        Assert.That(rootColumn.CanResize, Is.True);
    }

    [Test]
    public void ColumnWidthIsRememberedForSameColumnKind()
    {
        var widthStore = new PropertyColumnWidthStore();
        using var columns = CreateColumns(new ColumnRootTarget(), widthStore);

        columns.Columns[0].Width = 520;
        columns.OpenRoot(new ColumnRootTarget(), "Root");

        Assert.That(columns.Columns[0].Width, Is.EqualTo(520));
    }

    [Test]
    public void DrillInIsDefaultAndReplacesCurrentColumn()
    {
        using var explorer = CreateExplorer(new ColumnRootTarget());

        Assert.That(explorer.NavigationMode, Is.EqualTo(PropertyNavigationMode.DrillIn));

        explorer.RootObjectColumn!.SelectedProperty = FindProperty(
            explorer.RootObjectColumn,
            nameof(ColumnRootTarget.Child));

        Assert.That(explorer.DrillIn.CurrentColumn!.Title, Is.EqualTo(nameof(ColumnRootTarget.Child)));
        Assert.That(explorer.DrillIn.CanNavigateBack, Is.True);

        explorer.DrillIn.NavigateBack();

        Assert.That(explorer.DrillIn.CurrentColumn!.Title, Is.EqualTo("Root"));
        Assert.That(explorer.DrillIn.CanNavigateBack, Is.False);
    }

    [Test]
    public void SwitchingFromColumnsToDrillInUsesRightMostColumn()
    {
        using var explorer = CreateExplorer(new ColumnRootTarget());
        explorer.NavigationMode = PropertyNavigationMode.Columns;
        var rootColumn = explorer.Columns.RootObjectColumn!;
        rootColumn.SelectedProperty = FindProperty(rootColumn, nameof(ColumnRootTarget.Child));
        var childColumn = (ObjectPropertiesColumnViewModel)explorer.Columns.Columns[1].Content;
        childColumn.SelectedProperty = FindProperty(childColumn, nameof(ColumnChildTarget.GrandChild));

        explorer.NavigationMode = PropertyNavigationMode.DrillIn;

        Assert.That(explorer.DrillIn.CurrentColumn!.Title, Is.EqualTo(nameof(ColumnChildTarget.GrandChild)));
        Assert.That(explorer.DrillIn.CanNavigateBack, Is.True);

        explorer.DrillIn.NavigateBack();

        Assert.That(explorer.DrillIn.CurrentColumn!.Title, Is.EqualTo(nameof(ColumnRootTarget.Child)));
    }

    [Test]
    public void SwitchingFromDrillInToColumnsRestoresFullPath()
    {
        using var explorer = CreateExplorer(new ColumnRootTarget());
        explorer.RootObjectColumn!.SelectedProperty = FindProperty(explorer.RootObjectColumn, nameof(ColumnRootTarget.Child));
        var childColumn = (ObjectPropertiesColumnViewModel)explorer.DrillIn.CurrentColumn!.Content;
        childColumn.SelectedProperty = FindProperty(childColumn, nameof(ColumnChildTarget.GrandChild));

        explorer.NavigationMode = PropertyNavigationMode.Columns;

        Assert.That(explorer.Columns.Columns.Select(column => column.Title), Is.EqualTo(new[]
        {
            "Root",
            nameof(ColumnRootTarget.Child),
            nameof(ColumnChildTarget.GrandChild),
        }));
    }

    [Test]
    public void ContainerColumnPagesItemsByOneHundred()
    {
        var values = Enumerable.Range(0, 105).ToArray();
        using var columns = CreateColumns(values);
        var containerColumn = (ContainerPropertiesColumnViewModel)columns.Columns[0].Content;

        Assert.That(containerColumn.Items, Has.Count.EqualTo(100));
        Assert.That(containerColumn.CanShowMore, Is.True);

        containerColumn.ShowMore();

        Assert.That(containerColumn.Items, Has.Count.EqualTo(105));
        Assert.That(containerColumn.CanShowMore, Is.False);
    }

    [Test]
    public void EnumerableColumnDoesNotEnumerateUntilShowMore()
    {
        var enumerable = new CountingEnumerable(105);
        using var columns = CreateColumns(enumerable);
        var containerColumn = (ContainerPropertiesColumnViewModel)columns.Columns[0].Content;

        Assert.That(enumerable.MoveNextCount, Is.EqualTo(0));
        Assert.That(containerColumn.Items, Is.Empty);
        Assert.That(containerColumn.CanShowMore, Is.True);

        containerColumn.ShowMore();

        Assert.That(enumerable.MoveNextCount, Is.EqualTo(100));
        Assert.That(containerColumn.Items, Has.Count.EqualTo(100));
        Assert.That(containerColumn.CanShowMore, Is.True);
    }

    [Test]
    public void CloseFromColumnClosesCurrentAndRightColumns()
    {
        using var columns = CreateColumns(new ColumnRootTarget());
        var rootColumn = columns.RootObjectColumn!;

        rootColumn.SelectedProperty = FindProperty(rootColumn, nameof(ColumnRootTarget.Child));
        var childColumn = (ObjectPropertiesColumnViewModel)columns.Columns[1].Content;
        childColumn.SelectedProperty = FindProperty(childColumn, nameof(ColumnChildTarget.GrandChild));

        columns.Columns[1].Close();

        Assert.That(columns.Columns, Has.Count.EqualTo(1));
    }

    [Test]
    public void RefreshReinspectsColumn()
    {
        using var columns = CreateColumns(new ColumnRootTarget());
        var rootColumn = columns.RootObjectColumn!;
        var originalView = rootColumn.PropertiesView;

        rootColumn.Refresh();

        Assert.That(rootColumn.PropertiesView, Is.Not.SameAs(originalView));
        Assert.That(FindProperty(rootColumn, nameof(ColumnRootTarget.Name)), Is.Not.Null);
    }

    private static PropertyExplorerViewModel CreateExplorer(object target)
    {
        var explorer = new PropertyExplorerViewModel(CreateFactory());
        explorer.OpenRoot(target, "Root");
        return explorer;
    }

    private static PropertyColumnsViewModel CreateColumns(
        object target,
        PropertyColumnWidthStore? widthStore = null)
    {
        var columns = new PropertyColumnsViewModel(CreateFactory(), widthStore ?? new PropertyColumnWidthStore());
        columns.OpenRoot(target, "Root");
        return columns;
    }

    private static PropertyColumnFactory CreateFactory()
    {
        return new PropertyColumnFactory(
            PropertyInspector.Default,
            () => PropertyInspectionOptions.Default,
            () => false,
            property => property.IsPinned = !property.IsPinned);
    }

    private static PropertyViewModel FindProperty(ObjectPropertiesColumnViewModel column, string name)
    {
        return column.PropertiesView!
            .OfType<PropertyViewModel>()
            .Single(property => property.Name == name);
    }

    private sealed class ColumnRootTarget
    {
        public ColumnChildTarget Child { get; } = new();

        public ColumnChildTarget Other { get; } = new();

        public List<string> Children { get; } = ["first", "second"];

        public Dictionary<string, object> Lookup { get; } = new()
        {
            ["child"] = new ColumnChildTarget(),
            ["name"] = "value",
        };

        public string Name { get; set; } = "Root";
    }

    private sealed class ColumnChildTarget
    {
        public ColumnGrandChildTarget GrandChild { get; } = new();

        public string Name { get; set; } = "Child";
    }

    private sealed class ColumnGrandChildTarget
    {
        public string Name { get; set; } = "GrandChild";
    }

    private sealed class CountingEnumerable(int count) : IEnumerable<int>
    {

        public int MoveNextCount { get; private set; }

        public IEnumerator<int> GetEnumerator()
        {
            for (var index = 0; index < count; index++)
            {
                MoveNextCount++;
                yield return index;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
