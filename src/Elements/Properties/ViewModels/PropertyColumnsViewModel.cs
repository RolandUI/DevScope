using System.Collections.ObjectModel;
using RolandUI.DevScope.Elements.Properties.Models;
using RolandUI.DevScope.Elements.Properties.Services;
using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Elements.Properties.ViewModels;

internal sealed class PropertyColumnsViewModel(
    IPropertyColumnFactory columnFactory,
    PropertyColumnWidthStore widthStore
) : ReactiveViewModelBase, IPropertyColumnOwner
{
    private PropertyNavigationEntry? _rootEntry;

    public ObservableCollection<PropertyColumnViewModel> Columns { get; } = [];

    public bool HasMultipleColumns => Columns.Count > 1;

    public PropertyColumnViewModel? RootColumn => Columns.FirstOrDefault();

    public ObjectPropertiesColumnViewModel? RootObjectColumn =>
        RootColumn?.Content as ObjectPropertiesColumnViewModel;

    public void OpenRoot(object target, string title)
    {
        LoadPath([new PropertyNavigationEntry(target, title, title)]);
    }

    public void OpenFrom(PropertyColumnViewModel sourceColumn, PropertyViewModel? property)
    {
        OpenFrom(sourceColumn, property is null ? null : new PropertyColumnPropertyItemViewModel(property));
    }

    public void OpenFrom(PropertyColumnViewModel sourceColumn, IPropertyColumnItemViewModel? item)
    {
        var sourceIndex = Columns.IndexOf(sourceColumn);
        if (sourceIndex < 0)
        {
            return;
        }

        TrimAfter(sourceIndex);

        if (item?.CanNavigate != true || item.Value is null)
        {
            return;
        }

        var title = PropertyColumnNavigation.GetDisplayName(item.Name);
        Columns.Add(CreateColumn(new PropertyNavigationEntry(item.Value, title, $"{sourceColumn.Path}.{title}")));
        UpdateColumnLayout();
    }

    public void CloseFrom(PropertyColumnViewModel column)
    {
        var columnIndex = Columns.IndexOf(column);
        if (columnIndex < 0)
        {
            return;
        }

        TrimAfter(columnIndex - 1);
        UpdateColumnLayout();
    }

    public void RememberWidth(PropertyColumnViewModel column)
    {
        widthStore.Set(column.WidthKey, column.Width);
    }

    public void RefreshAll()
    {
        foreach (var column in Columns)
        {
            column.Refresh();
        }
    }

    public void RefreshViews()
    {
        foreach (var objectColumn in Columns.Select(column => column.Content).OfType<ObjectPropertiesColumnViewModel>())
        {
            objectColumn.RefreshView();
        }
    }

    public void SelectPropertyInRoot(AvaloniaProperty property)
    {
        RootObjectColumn?.SelectProperty(property);
    }

    public void Reset()
    {
        if (_rootEntry is { } rootEntry)
        {
            LoadPath([rootEntry]);
        }
    }

    public IReadOnlyList<PropertyNavigationEntry> GetPath()
    {
        return Columns
            .Select(column => new PropertyNavigationEntry(column.Content.Target, column.Title, column.Path))
            .ToList();
    }

    public void LoadPath(IReadOnlyList<PropertyNavigationEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        _rootEntry = entries[0];
        ClearColumns();

        foreach (var entry in entries)
        {
            Columns.Add(CreateColumn(entry));
        }

        UpdateColumnLayout();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearColumns();
        }

        base.Dispose(disposing);
    }

    private PropertyColumnViewModel CreateColumn(PropertyNavigationEntry entry)
    {
        var column = columnFactory.CreateColumn(this, entry.Target, entry.Title, entry.Path);

        if (widthStore.TryGet(column.WidthKey, out var width))
        {
            column.SetRememberedWidth(width);
        }

        return column;
    }

    private void ClearColumns()
    {
        foreach (var column in Columns.ToArray())
        {
            column.Dispose();
        }

        Columns.Clear();
    }

    private void TrimAfter(int columnIndex)
    {
        for (var index = Columns.Count - 1; index > columnIndex; index--)
        {
            var column = Columns[index];
            Columns.RemoveAt(index);
            column.Dispose();
        }

        UpdateColumnLayout();
    }

    private void UpdateColumnLayout()
    {
        foreach (var column in Columns)
        {
            column.CanResize = true;
            column.CanClose = Columns.IndexOf(column) > 0;
        }

        RaisePropertyChanged(nameof(HasMultipleColumns));
    }
}