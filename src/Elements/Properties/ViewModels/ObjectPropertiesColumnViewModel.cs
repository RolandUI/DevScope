using System.Collections;
using System.ComponentModel;
using Avalonia.Collections;
using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;

internal sealed class ObjectPropertiesColumnViewModel : ReactiveViewModelBase, IPropertyColumnContentViewModel
{
    private readonly static IReadOnlyList<DataGridPathGroupDescription> GroupDescriptors =
    [
        new(nameof(AvaloniaPropertyViewModel.Group)),
    ];

    private readonly static IReadOnlyList<DataGridSortDescription> SortDescriptions =
    [
        new DataGridComparerSortDescription(PropertyComparer.Instance, ListSortDirection.Ascending),
    ];

    private readonly IPropertyInspector _propertyInspector;
    private readonly Func<PropertyInspectionOptions> _createOptions;
    private readonly Func<bool> _showDetailsPropertyType;
    private readonly Action<PropertyViewModel> _togglePinnedProperty;
    private IReadOnlyDictionary<object, PropertyViewModel[]>? _propertyIndex;

    public ObjectPropertiesColumnViewModel(
        object target,
        string title,
        string path,
        IPropertyInspector propertyInspector,
        Func<PropertyInspectionOptions> createOptions,
        Func<bool> showDetailsPropertyType,
        Action<PropertyViewModel> togglePinnedProperty)
    {
        Target = target;
        Title = title;
        Path = path;
        _propertyInspector = propertyInspector;
        _createOptions = createOptions;
        _showDetailsPropertyType = showDetailsPropertyType;
        _togglePinnedProperty = togglePinnedProperty;

        Filter.RefreshFilter += HandleFilterRefreshFilter;
        Refresh();
        SubscribeToTargetChanges();
    }

    public event EventHandler<PropertyViewModel?>? SelectedPropertyChanged;

    public object Target { get; }

    public string Title { get; }

    public string Path { get; }

    public FilterViewModel Filter { get; } = new();

    public bool ShowDetailsPropertyType => _showDetailsPropertyType();

    public DataGridCollectionView? PropertiesView
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public PropertyViewModel? SelectedProperty
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SelectedPropertyChanged?.Invoke(this, value);
            }
        }
    }

    public void Refresh()
    {
        var inspection = _propertyInspector.Inspect(Target, _createOptions());
        _propertyIndex = inspection.PropertyIndex;

        var view = new DataGridCollectionView(inspection.Properties);
        view.GroupDescriptions.AddRange(GroupDescriptors);
        view.SortDescriptions.AddRange(SortDescriptions);
        view.Filter = FilterProperty;

        SelectedProperty = null;
        PropertiesView = view;
    }

    public void RefreshView()
    {
        PropertiesView?.Refresh();
    }

    public void TogglePinnedProperty(object parameter)
    {
        if (parameter is PropertyViewModel property)
        {
            _togglePinnedProperty(property);
        }
    }

    public void SelectProperty(AvaloniaProperty property)
    {
        SelectedProperty = null;

        if (PropertiesView is null)
        {
            return;
        }

        foreach (var item in PropertiesView)
        {
            if (item is AvaloniaPropertyViewModel propertyViewModel && propertyViewModel.Property == property)
            {
                SelectedProperty = propertyViewModel;
                break;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnsubscribeFromTargetChanges();
            Filter.RefreshFilter -= HandleFilterRefreshFilter;
        }

        base.Dispose(disposing);
    }

    private void SubscribeToTargetChanges()
    {
        switch (Target)
        {
            case AvaloniaObject avaloniaObject:
                avaloniaObject.PropertyChanged += HandleTargetPropertyChanged;
                break;

            case INotifyPropertyChanged notifyPropertyChanged:
                notifyPropertyChanged.PropertyChanged += HandleTargetPropertyChanged;
                break;
        }
    }

    private void UnsubscribeFromTargetChanges()
    {
        switch (Target)
        {
            case AvaloniaObject avaloniaObject:
                avaloniaObject.PropertyChanged -= HandleTargetPropertyChanged;
                break;

            case INotifyPropertyChanged notifyPropertyChanged:
                notifyPropertyChanged.PropertyChanged -= HandleTargetPropertyChanged;
                break;
        }
    }

    private void HandleTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (_propertyIndex is not null && _propertyIndex.TryGetValue(args.Property, out var properties))
        {
            foreach (var property in properties)
            {
                property.Update();
            }
        }
    }

    private void HandleTargetPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is not null
            && _propertyIndex is not null
            && _propertyIndex.TryGetValue(args.PropertyName, out var properties))
        {
            foreach (var property in properties)
            {
                property.Update();
            }
        }
    }

    private void HandleFilterRefreshFilter(object? sender, EventArgs args)
    {
        PropertiesView?.Refresh();
    }

    private bool FilterProperty(object item)
    {
        return item is not PropertyViewModel property || Filter.Filter(property.Name);
    }

    private sealed class PropertyComparer : IComparer<PropertyViewModel>, IComparer
    {
        public static PropertyComparer Instance { get; } = new();

        public int Compare(object? x, object? y)
        {
            return Compare(x as PropertyViewModel, y as PropertyViewModel);
        }

        public int Compare(PropertyViewModel? x, PropertyViewModel? y)
        {
            if (x is null && y is null)
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var groupX = GroupIndex(x.Group);
            var groupY = GroupIndex(y.Group);

            return groupX != groupY ? groupX - groupY : string.CompareOrdinal(x.Name, y.Name);
        }

        private static int GroupIndex(string? group)
        {
            return group switch
            {
                "Pinned" => -1,
                "Properties" => 0,
                "Attached Properties" => 1,
                "Items" or "Entries" => 2,
                "CLR Properties" => 3,
                _ => 4,
            };
        }
    }
}