using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

internal sealed class PropertyDrillInViewModel(
    IPropertyColumnFactory columnFactory,
    PropertyColumnWidthStore widthStore
) : ReactiveViewModelBase
{
    private readonly Stack<PropertyNavigationEntry> _history = new();
    private PropertyNavigationEntry? _rootEntry;

    public bool CanNavigateBack => _history.Count > 0;

    public PropertyColumnViewModel? CurrentColumn
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public ObjectPropertiesColumnViewModel? RootObjectColumn =>
        CurrentColumn?.Content as ObjectPropertiesColumnViewModel;

    public void OpenRoot(object target, string title)
    {
        LoadPath([new PropertyNavigationEntry(target, title, title)]);
    }

    public void OpenFrom(PropertyViewModel property)
    {
        OpenFrom(new PropertyColumnPropertyItemViewModel(property));
    }

    public void OpenFrom(IPropertyColumnItemViewModel item)
    {
        if (CurrentColumn is null || !item.CanNavigate || item.Value is null)
        {
            return;
        }

        _history.Push(new PropertyNavigationEntry(CurrentColumn.Content.Target, CurrentColumn.Title, CurrentColumn.Path));
        var title = PropertyColumnNavigation.GetDisplayName(item.Name);
        ReplaceColumn(new PropertyNavigationEntry(item.Value, title, $"{CurrentColumn.Path}.{title}"));
        RaisePropertyChanged(nameof(CanNavigateBack));
    }

    public void NavigateBack()
    {
        if (_history.Count == 0)
        {
            return;
        }

        ReplaceColumn(_history.Pop());
        RaisePropertyChanged(nameof(CanNavigateBack));
    }

    public void RefreshViews()
    {
        if (CurrentColumn?.Content is ObjectPropertiesColumnViewModel objectColumn)
        {
            objectColumn.RefreshView();
        }
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
        if (CurrentColumn is null)
        {
            return [];
        }

        var currentEntry = new PropertyNavigationEntry(CurrentColumn.Content.Target, CurrentColumn.Title, CurrentColumn.Path);
        return _history.Reverse().Append(currentEntry).ToArray();
    }

    public void LoadPath(IReadOnlyList<PropertyNavigationEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        _rootEntry = entries[0];
        _history.Clear();

        foreach (var entry in entries.Take(entries.Count - 1))
        {
            _history.Push(entry);
        }

        ReplaceColumn(entries[^1]);
        RaisePropertyChanged(nameof(CanNavigateBack));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CurrentColumn?.Dispose();
            CurrentColumn = null;
        }

        base.Dispose(disposing);
    }

    private void ReplaceColumn(PropertyNavigationEntry entry)
    {
        CurrentColumn?.Dispose();
        CurrentColumn = CreateColumn(entry);
    }

    private PropertyColumnViewModel CreateColumn(PropertyNavigationEntry entry)
    {
        var column = columnFactory.CreateColumn(new DrillInColumnOwner(this, widthStore), entry.Target, entry.Title, entry.Path);
        column.CanClose = false;
        column.CanResize = true;
        return column;
    }

    private sealed class DrillInColumnOwner(PropertyDrillInViewModel owner, PropertyColumnWidthStore widthStore) : IPropertyColumnOwner
    {
        public void OpenFrom(PropertyColumnViewModel sourceColumn, PropertyViewModel? property)
        {
            if (property is not null)
            {
                owner.OpenFrom(property);
            }
        }

        public void OpenFrom(PropertyColumnViewModel sourceColumn, IPropertyColumnItemViewModel? item)
        {
            if (item is not null)
            {
                owner.OpenFrom(item);
            }
        }

        public void CloseFrom(PropertyColumnViewModel column)
        {
            owner.NavigateBack();
        }

        public void RememberWidth(PropertyColumnViewModel column)
        {
            widthStore.Set(column.WidthKey, column.Width);
        }
    }
}