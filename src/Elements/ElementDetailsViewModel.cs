using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls.Metadata;
using Avalonia.Threading;
using RolandUI.DevScope.Elements.Properties;
using RolandUI.DevScope.Elements.Properties.Services;
using RolandUI.DevScope.Elements.Properties.ViewModels;
using RolandUI.DevScope.Elements.Styles;
using RolandUI.DevScope.Elements.Trees;
using RolandUI.DevScope.ViewModels;
using DataGridCollectionView = Avalonia.Collections.DataGridCollectionView;

namespace RolandUI.DevScope.Elements;

internal class ElementDetailsViewModel : ReactiveViewModelBase, IClassesChangedListener
{
    public bool CanNavigateToParentProperty => false;

    public ElementsTreeViewModel TreePage { get; }

    public DataGridCollectionView? PropertiesView => PropertyExplorer.RootObjectColumn?.PropertiesView;

    public ObservableCollection<ValueFrameViewModel> AppliedFrames { get; }

    public ObservableCollection<StyleClassViewModel> Classes { get; }

    public ObservableCollection<PseudoClassViewModel> PseudoClasses { get; }

    public bool HasStyleClassSection => _styledElement is not null;

    public object SelectedEntity => _avaloniaObject;

    public string? SelectedEntityName => PropertyExplorer.RootObjectColumn?.Title;

    public string? SelectedEntityType => _avaloniaObject.ToString();

    public PropertyViewModel? SelectedProperty
    {
        get => PropertyExplorer.RootObjectColumn?.SelectedProperty;
        set
        {
            if (PropertyExplorer.RootObjectColumn is { } rootColumn)
            {
                rootColumn.SelectedProperty = value;
            }
        }
    }

    public bool FreezeFrames
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

    public ElementLayoutViewModel? Layout { get; }

    public PropertyExplorerViewModel PropertyExplorer { get; }

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

    private readonly AvaloniaObject _avaloniaObject;
    private readonly IPinnedPropertyStore _pinnedProperties;
    private readonly StyledElement? _styledElement;
    private bool _showImplementedInterfaces;

    public ElementDetailsViewModel(ElementsTreeViewModel treePage, AvaloniaObject avaloniaObject, IPinnedPropertyStore pinnedProperties)
    {
        _avaloniaObject = avaloniaObject;
        _pinnedProperties = pinnedProperties;
        _styledElement = avaloniaObject as StyledElement;
        TreePage = treePage;
        Layout = avaloniaObject is Visual visual ? new ElementLayoutViewModel(visual) : null;
        PropertyExplorer = new PropertyExplorerViewModel(
            new PropertyColumnFactory(
                PropertyInspector.Default,
                () => new PropertyInspectionOptions(_showImplementedInterfaces, _pinnedProperties),
                () => TreePage.MainView.ShowDetailsPropertyType,
                TogglePinnedProperty));

        PropertyExplorer.OpenRoot(_avaloniaObject, GetRootTitle());
        _avaloniaObject.PropertyChanged += HandleRootPropertyChanged;

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

            foreach (var appliedStyle in _styledElement.GetValueStoreDiagnostic().AppliedFrames.OrderBy(s => s.Priority))
            {
                AppliedFrames.Add(new ValueFrameViewModel(_styledElement, appliedStyle, clipboard));
            }

            UpdateStyles();
        }

        if (_avaloniaObject is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += HandleRootPropertyChanged;
        }
    }

    void IClassesChangedListener.Changed()
    {
        RefreshClasses();
        AddActivePseudoClasses();

        if (!FreezeFrames)
        {
            Dispatcher.UIThread.Post(UpdateStyles);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(FreezeFrames))
        {
            if (!FreezeFrames)
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
            notifyPropertyChanged.PropertyChanged -= HandleRootPropertyChanged;
        }

        _avaloniaObject.PropertyChanged -= HandleRootPropertyChanged;
        PropertyExplorer.Dispose();

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

    private void HandleRootPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        Layout?.ControlPropertyChanged(sender, e);
    }

    private void HandleRootPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!FreezeFrames)
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
        PropertyExplorer.OpenSelectedFromRoot();
    }

    public void NavigateToParentProperty()
    {
    }

    internal void SelectProperty(AvaloniaProperty property)
    {
        PropertyExplorer.SelectPropertyInRoot(property);
    }

    internal void UpdatePropertiesView(bool showImplementedInterfaces)
    {
        _showImplementedInterfaces = showImplementedInterfaces;
        PropertyExplorer.OpenRoot(_avaloniaObject, GetRootTitle());
        RaisePropertyChanged(nameof(PropertiesView));
        RaisePropertyChanged(nameof(SelectedEntityName));
    }

    public void TogglePinnedProperty(object parameter)
    {
        if (parameter is PropertyViewModel property)
        {
            TogglePinnedProperty(property);
        }
    }

    private void TogglePinnedProperty(PropertyViewModel property)
    {
        property.IsPinned = _pinnedProperties.Toggle(property.FullName);
        PropertyExplorer.RefreshViews();
    }

    private string GetRootTitle()
    {
        return (_avaloniaObject as Control)?.Name ?? _avaloniaObject.GetType().GetTypeName();
    }
}