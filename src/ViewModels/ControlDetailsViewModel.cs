using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Collections;
using Avalonia.Controls.Metadata;
using Avalonia.Threading;
using ClassicDiagnostics.Avalonia.Properties;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class ControlDetailsViewModel : ReactiveViewModelBase, IClassesChangedListener
{
    // new DataGridPathGroupDescription(nameof(AvaloniaPropertyViewModel.Group))
    private readonly static IReadOnlyList<DataGridPathGroupDescription> GroupDescriptors =
    [
        new(nameof(AvaloniaPropertyViewModel.Group)),
    ];

    private readonly static IReadOnlyList<DataGridSortDescription> SortDescriptions =
    [
        new DataGridComparerSortDescription(PropertyComparer.Instance, ListSortDirection.Ascending),
    ];

    private readonly AvaloniaObject _avaloniaObject;
    private readonly IPropertyInspector _propertyInspector;
    private readonly IPinnedPropertyStore _pinnedProperties;
    private readonly Stack<(string Name, object Entry)> _selectedEntitiesStack = new();
    private readonly StyledElement? _styledElement;
    private IReadOnlyDictionary<object, PropertyViewModel[]>? _propertyIndex;
    private object? _selectedEntity;
    private bool _showImplementedInterfaces;

    public ControlDetailsViewModel(TreePageViewModel treePage, AvaloniaObject avaloniaObject, IPinnedPropertyStore pinnedProperties)
    {
        _avaloniaObject = avaloniaObject;
        _pinnedProperties = pinnedProperties;
        _propertyInspector = PropertyInspector.Default;
        _styledElement = avaloniaObject as StyledElement;
        TreePage = treePage;
        Layout = avaloniaObject is Visual visual ? new ControlLayoutViewModel(visual) : null;

        NavigateToProperty(_avaloniaObject, (_avaloniaObject as Control)?.Name ?? _avaloniaObject.ToString());

        AppliedFrames = [];
        Classes = [];
        PseudoClasses = [];

        if (_styledElement is not null)
        {
            _styledElement.Classes.AddListener(this);
            RefreshClasses();

            var pseudoClassAttributes = _styledElement.GetType().GetCustomAttributes<PseudoClassesAttribute>(true);

            foreach (var classAttribute in pseudoClassAttributes)
            {
                foreach (var className in classAttribute.PseudoClasses)
                {
                    AddPseudoClassViewModel(className);
                }
            }
            AddActivePseudoClasses();

            var clipboard = TopLevel.GetTopLevel(_avaloniaObject as Visual)?.Clipboard;

            foreach (var appliedStyle in AvaloniaPrivateApi.Current.GetAppliedStyleFrames(_styledElement).OrderBy(s => s.Priority))
            {
                AppliedFrames.Add(new ValueFrameViewModel(_styledElement, appliedStyle, clipboard));
            }

            UpdateStyles();
        }
    }

    public bool CanNavigateToParentProperty => _selectedEntitiesStack.Count >= 1;

    public TreePageViewModel TreePage { get; }

    public DataGridCollectionView? PropertiesView
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public ObservableCollection<ValueFrameViewModel> AppliedFrames { get; }

    public ObservableCollection<StyleClassViewModel> Classes { get; }

    public ObservableCollection<PseudoClassViewModel> PseudoClasses { get; }

    public bool HasStyleClassSection => _styledElement is not null;

    public object? SelectedEntity
    {
        get => _selectedEntity;
        set => SetProperty(ref _selectedEntity, value);
    }

    public string? SelectedEntityName
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? SelectedEntityType
    {
        get;
        set => SetProperty(ref field, value);
    }

    public PropertyViewModel? SelectedProperty
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool SnapshotFrames
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShowInactiveFrames
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? FramesStatus
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ControlLayoutViewModel? Layout { get; }

    public string? ClassEditError
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string NewClassName
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public string? PseudoClassEditError
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string NewPseudoClassName
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    void IClassesChangedListener.Changed()
    {
        RefreshClasses();
        AddActivePseudoClasses();

        if (!SnapshotFrames)
        {
            Dispatcher.UIThread.Post(UpdateStyles);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SnapshotFrames))
        {
            if (!SnapshotFrames)
            {
                UpdateStyles();
            }
        }
    }

    public void UpdateStyleFilters()
    {
        foreach (var style in AppliedFrames)
        {
            var hasVisibleSetter = false;

            foreach (var setter in style.Setters)
            {
                setter.IsVisible = TreePage.SettersFilter.Filter(setter.Name);

                hasVisibleSetter |= setter.IsVisible;
            }

            style.IsVisible = hasVisibleSetter;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(disposing);
            return;
        }

        if (_avaloniaObject is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged -= ControlPropertyChanged;
        }

        _avaloniaObject.PropertyChanged -= ControlPropertyChanged;

        if (_styledElement is not null)
        {
            _styledElement.Classes.RemoveListener(this);
        }

        base.Dispose(disposing);
    }

    public void AddClass()
    {
        if (_styledElement is null)
        {
            return;
        }

        var className = NormalizeClassName(NewClassName);
        if (className.Length == 0)
        {
            ClassEditError = "Class name is required.";
            return;
        }

        if (className.StartsWith(':'))
        {
            ClassEditError = "Pseudo classes must be added in the Pseudo Classes section.";
            return;
        }

        if (_styledElement.Classes.Contains(className))
        {
            ClassEditError = $"Class '{className}' already exists.";
            return;
        }

        try
        {
            _styledElement.Classes.Add(className);
            NewClassName = string.Empty;
            ClassEditError = null;
            RefreshClasses();
        }
        catch (Exception exception)
        {
            ClassEditError = exception.Message;
            DevToolsDiagnostics.Report(exception, $"Failed to add class '{className}'.");
        }
    }

    public void AddPseudoClass()
    {
        if (_styledElement is null)
        {
            return;
        }

        var pseudoClassName = NormalizePseudoClassName(NewPseudoClassName);
        if (pseudoClassName.Length <= 1)
        {
            PseudoClassEditError = "Pseudo class name is required.";
            return;
        }

        var pseudoClass = AddPseudoClassViewModel(pseudoClassName);
        pseudoClass.IsActive = true;
        if (pseudoClass.Error is not null)
        {
            PseudoClassEditError = pseudoClass.Error;
            return;
        }

        NewPseudoClassName = string.Empty;
        PseudoClassEditError = null;
    }

    internal void RemoveClass(StyleClassViewModel styleClass)
    {
        if (_styledElement is null)
        {
            return;
        }

        try
        {
            _styledElement.Classes.Remove(styleClass.Name);
            ClassEditError = null;
            RefreshClasses();
        }
        catch (Exception exception)
        {
            ClassEditError = exception.Message;
            DevToolsDiagnostics.Report(exception, $"Failed to remove class '{styleClass.Name}'.");
        }
    }

    private void ControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_propertyIndex is not null && _propertyIndex.TryGetValue(e.Property, out var properties))
        {
            foreach (var property in properties)
            {
                property.Update();
            }
        }

        Layout?.ControlPropertyChanged(sender, e);
    }

    private void ControlPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null
            && _propertyIndex is not null && _propertyIndex.TryGetValue(e.PropertyName, out var properties))
        {
            foreach (var property in properties)
            {
                property.Update();
            }
        }

        if (!SnapshotFrames)
        {
            Dispatcher.UIThread.Post(UpdateStyles);
        }
    }

    private void UpdateStyles()
    {
        var activeCount = 0;

        foreach (var style in AppliedFrames)
        {
            style.Update();

            if (style.IsActive)
            {
                activeCount++;
            }
        }

        var propertyBuckets = new Dictionary<AvaloniaProperty, List<SetterViewModel>>();

        foreach (var style in AppliedFrames.Reverse())
        {
            if (!style.IsActive)
            {
                continue;
            }

            foreach (var setter in style.Setters)
            {
                if (propertyBuckets.TryGetValue(setter.Property, out var setters))
                {
                    foreach (var otherSetter in setters)
                    {
                        otherSetter.IsActive = false;
                    }

                    setter.IsActive = true;

                    setters.Add(setter);
                }
                else
                {
                    setter.IsActive = true;

                    setters = new List<SetterViewModel> { setter };

                    propertyBuckets.Add(setter.Property, setters);
                }
            }
        }

        foreach (var pseudoClass in PseudoClasses)
        {
            pseudoClass.Update();
        }

        FramesStatus = $"Value Frames ({activeCount}/{AppliedFrames.Count} active)";
    }

    private PseudoClassViewModel AddPseudoClassViewModel(string name)
    {
        var normalizedName = NormalizePseudoClassName(name);
        if (PseudoClasses.FirstOrDefault(x => x.Name == normalizedName) is { } existing)
        {
            return existing;
        }

        var pseudoClass = new PseudoClassViewModel(normalizedName, _styledElement!);
        PseudoClasses.Add(pseudoClass);
        return pseudoClass;
    }

    private void AddActivePseudoClasses()
    {
        if (_styledElement is null)
        {
            return;
        }

        foreach (var className in _styledElement.Classes.Where(x => x.StartsWith(':')))
        {
            AddPseudoClassViewModel(className).Update();
        }
    }

    private void RefreshClasses()
    {
        if (_styledElement is null)
        {
            return;
        }

        Classes.Clear();
        foreach (var className in _styledElement.Classes.Where(x => !x.StartsWith(':')).OrderBy(x => x))
        {
            Classes.Add(new StyleClassViewModel(className, this));
        }
    }

    private bool FilterProperty(object arg)
    {
        return !(arg is PropertyViewModel property) || TreePage.PropertiesFilter.Filter(property.Name);
    }

    private static string NormalizeClassName(string? className)
    {
        var normalizedName = className?.Trim() ?? string.Empty;
        return normalizedName.StartsWith('.') ? normalizedName[1..].Trim() : normalizedName;
    }

    private static string NormalizePseudoClassName(string? pseudoClassName)
    {
        var normalizedName = pseudoClassName?.Trim() ?? string.Empty;
        return normalizedName.StartsWith(':') ? normalizedName : $":{normalizedName}";
    }

    public void NavigateToSelectedProperty()
    {
        var selectedProperty = SelectedProperty;
        var selectedEntity = SelectedEntity;
        var selectedEntityName = SelectedEntityName;
        if (selectedEntity == null || selectedProperty == null)
            return;

        var property = selectedProperty.Value;
        var descriptor = PropertyValueDescriptorFactory.Default.Create(property);
        if (!descriptor.CanNavigate || property is null)
        {
            return;
        }

        _selectedEntitiesStack.Push((Name: selectedEntityName!, Entry: selectedEntity));

        var propertyName = selectedProperty.Name;

        //Strip out interface names
        if (propertyName.LastIndexOf('.') is var p && p != -1)
        {
            propertyName = propertyName.Substring(p + 1);
        }

        NavigateToProperty(property, selectedEntityName + "." + propertyName);

        RaisePropertyChanged(nameof(CanNavigateToParentProperty));
    }

    public void NavigateToParentProperty()
    {
        if (_selectedEntitiesStack.Count > 0)
        {
            var property = _selectedEntitiesStack.Pop();
            NavigateToProperty(property.Entry, property.Name);

            RaisePropertyChanged(nameof(CanNavigateToParentProperty));
        }
    }

    protected void NavigateToProperty(object obj, string? entityName)
    {
        var oldSelectedEntity = SelectedEntity;

        switch (oldSelectedEntity)
        {
            case AvaloniaObject oldAvaloniaObject:
                oldAvaloniaObject.PropertyChanged -= ControlPropertyChanged;
                break;

            case INotifyPropertyChanged oldNotifyPropertyChanged:
                oldNotifyPropertyChanged.PropertyChanged -= ControlPropertyChanged;
                break;
        }

        SelectedEntity = obj;
        SelectedEntityName = entityName;
        SelectedEntityType = obj.ToString();

        var inspection = _propertyInspector.Inspect(
            obj,
            new PropertyInspectionOptions(_showImplementedInterfaces, _pinnedProperties));

        _propertyIndex = inspection.PropertyIndex;

        var view = new DataGridCollectionView(inspection.Properties);
        view.GroupDescriptions.AddRange(GroupDescriptors);
        view.SortDescriptions.AddRange(SortDescriptions);
        view.Filter = FilterProperty;
        PropertiesView = view;

        switch (obj)
        {
            case AvaloniaObject avaloniaObject:
                avaloniaObject.PropertyChanged += ControlPropertyChanged;
                break;

            case INotifyPropertyChanged notifyPropertyChanged:
                notifyPropertyChanged.PropertyChanged += ControlPropertyChanged;
                break;
        }
    }

    internal void SelectProperty(AvaloniaProperty property)
    {
        SelectedProperty = null;

        if (!Equals(SelectedEntity, _avaloniaObject))
        {
            NavigateToProperty(
                _avaloniaObject,
                (_avaloniaObject as Control)?.Name ?? _avaloniaObject.ToString());
        }

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

    internal void UpdatePropertiesView(bool showImplementedInterfaces)
    {
        _showImplementedInterfaces = showImplementedInterfaces;
        SelectedProperty = null;
        NavigateToProperty(_avaloniaObject, (_avaloniaObject as Control)?.Name ?? _avaloniaObject.ToString());
    }

    public void TogglePinnedProperty(object parameter)
    {
        if (parameter is PropertyViewModel model)
        {
            model.IsPinned = _pinnedProperties.Toggle(model.FullName);
            PropertiesView?.Refresh();
        }
    }

    private class PropertyComparer : IComparer<PropertyViewModel>, IComparer
    {
        public static PropertyComparer Instance { get; } = new();

        public int Compare(object? x, object? y)
        {
            return Compare(x as PropertyViewModel, y as PropertyViewModel);
        }

        public int Compare(PropertyViewModel? x, PropertyViewModel? y)
        {
            if (x is null && y is null)
                return 0;

            if (x is null && y is not null)
                return -1;

            if (x is not null && y is null)
                return 1;

            var groupX = GroupIndex(x!.Group);
            var groupY = GroupIndex(y!.Group);

            if (groupX != groupY)
            {
                return groupX - groupY;
            }
            return string.CompareOrdinal(x.Name, y.Name);
        }

        private static int GroupIndex(string? group)
        {
            switch (group)
            {
                case "Pinned":
                    return -1;
                case "Properties":
                    return 0;
                case "Attached Properties":
                    return 1;
                case "Items":
                case "Entries":
                    return 2;
                case "CLR Properties":
                    return 3;
                default:
                    return 4;
            }
        }
    }
}
