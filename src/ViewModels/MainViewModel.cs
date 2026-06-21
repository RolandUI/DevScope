using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Rendering;
using Avalonia.Threading;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Elements;
using ClassicDiagnostics.Avalonia.Properties;
using ClassicDiagnostics.Avalonia.Tree;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class MainViewModel : ReactiveViewModelBase
{
    private readonly ElementsPageViewModel _elements;
    private readonly EventsPageViewModel _events;
    private readonly TreePageViewModel _logicalTree;
    private readonly IPinnedPropertyStore _pinnedProperties = new PinnedPropertyStore();
    private readonly AvaloniaObject _root;
    private readonly ISelectionCoordinator _selectionCoordinator;
    private readonly SettingsPageViewModel _settings;
    private readonly TracePageViewModel _trace;
    private readonly TreePageViewModel _visualTree;
    private IDisposable? _currentFocusHighlightAdorner;
    private DevToolsViewKind _selectedViewKind;
    private IScreenshotHandler? _screenshotHandler;

    public MainViewModel(AvaloniaObject root)
    {
        _root = root;
        _selectionCoordinator = new SelectionCoordinator(
            _pinnedProperties,
            () => ShowImplementedInterfaces,
            SelectTreeFromCoordinator);
        _logicalTree = new TreePageViewModel(this, new LogicalTreeProvider().Create(root), _selectionCoordinator);
        _visualTree = new TreePageViewModel(this, new VisualTreeProvider().Create(root), _selectionCoordinator);
        _selectionCoordinator.Attach(_logicalTree, _visualTree);
        _elements = new ElementsPageViewModel(_logicalTree, _visualTree, _selectionCoordinator);
        _events = new EventsPageViewModel(this);
        _trace = new TracePageViewModel();
        _settings = new SettingsPageViewModel();
        Tabs =
        [
            new DevToolsTabItemViewModel(this, DevToolsViewKind.Elements, "Elements", "LucideMousePointerClick"),
            new DevToolsTabItemViewModel(this, DevToolsViewKind.Events, "Events", "LucideRadioTower"),
            new DevToolsTabItemViewModel(this, DevToolsViewKind.Trace, "Trace", "LucideSquareTerminal"),
            new DevToolsTabItemViewModel(this, DevToolsViewKind.Settings, "Settings", "LucideSettings"),
        ];

        UpdateFocusedControl();

        var keyboard = KeyboardDevice.Instance;
        if (keyboard is not null)
        {
            keyboard.PropertyChanged += KeyboardPropertyChanged;
            Disposable.Create(() => keyboard.PropertyChanged -= KeyboardPropertyChanged)
                .AddTo(LifetimeDisposables);
        }

        SelectView(DevToolsViewKind.Elements);
        InputManager.Instance!.PreProcess
            .Subscribe(e =>
            {
                if (e is RawPointerEventArgs pointerEventArgs)
                {
                    PointerOverRoot = pointerEventArgs.Root;
                    PointerOverElement = pointerEventArgs.Root.PointerOverElement;
                }
            })
            .AddTo(LifetimeDisposables);
    }

    public bool FreezePopups
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShouldVisualizeMarginPadding
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public bool ShowDirtyRectsOverlay
    {
        get => GetDebugOverlay(RendererDebugOverlays.DirtyRects);
        set => SetDebugOverlay(RendererDebugOverlays.DirtyRects, value);
    }

    public bool ShowFpsOverlay
    {
        get => GetDebugOverlay(RendererDebugOverlays.Fps);
        set => SetDebugOverlay(RendererDebugOverlays.Fps, value);
    }

    public bool ShowLayoutTimeGraphOverlay
    {
        get => GetDebugOverlay(RendererDebugOverlays.LayoutTimeGraph);
        set => SetDebugOverlay(RendererDebugOverlays.LayoutTimeGraph, value);
    }

    public bool ShowRenderTimeGraphOverlay
    {
        get => GetDebugOverlay(RendererDebugOverlays.RenderTimeGraph);
        set => SetDebugOverlay(RendererDebugOverlays.RenderTimeGraph, value);
    }

    public ViewModelBase? Content
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public IReadOnlyList<DevToolsTabItemViewModel> Tabs { get; }

    public DevToolsViewKind SelectedViewKind
    {
        get => _selectedViewKind;
        private set => SetProperty(ref _selectedViewKind, value);
    }

    public bool IsElementsSelected => SelectedViewKind == DevToolsViewKind.Elements;

    public bool IsEventsSelected => SelectedViewKind == DevToolsViewKind.Events;

    public bool IsTraceSelected => SelectedViewKind == DevToolsViewKind.Trace;

    public bool IsSettingsSelected => SelectedViewKind == DevToolsViewKind.Settings;

    public string? FocusedControl
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public IInputRoot? PointerOverRoot
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public IInputElement? PointerOverElement
    {
        get;
        private set
        {
            SetProperty(ref field, value);
            PointerOverElementName = value?.GetType()?.Name;
        }
    }

    public string? PointerOverElementName
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public int? StartupScreenIndex { get; private set; }

    public bool ShowImplementedInterfaces
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public bool ShowDetailsPropertyType
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public IBrush? FocusHighlighter
    {
        get;
        private set => SetProperty(ref field, value);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(disposing);
            return;
        }

        _elements.Dispose();
        _logicalTree.Dispose();
        _visualTree.Dispose();
        _events.Dispose();
        _currentFocusHighlightAdorner?.Dispose();
        if (TryGetRenderer() is { } renderer)
        {
            renderer.Diagnostics.DebugOverlays = RendererDebugOverlays.None;
        }
        if (_root is PresentationRootGroup presentationRootGroup)
        {
            presentationRootGroup.Dispose();
        }

        base.Dispose(disposing);
    }

    public void ToggleVisualizeMarginPadding()
    {
        ShouldVisualizeMarginPadding = !ShouldVisualizeMarginPadding;
    }

    private IRenderer? TryGetRenderer()
    {
        return _root switch
        {
            TopLevel topLevel => topLevel.Renderer,
            ApplicationPage app => app.RendererRoot,
            _ => null,
        };
    }

    private bool GetDebugOverlay(RendererDebugOverlays overlay)
    {
        return ((TryGetRenderer()?.Diagnostics.DebugOverlays ?? RendererDebugOverlays.None) & overlay) != 0;
    }

    private void SetDebugOverlay(
        RendererDebugOverlays overlay,
        bool enable,
        [CallerMemberName] string? propertyName = null)
    {
        if (TryGetRenderer() is not { } renderer)
        {
            return;
        }

        var oldValue = renderer.Diagnostics.DebugOverlays;
        var newValue = enable ? oldValue | overlay : oldValue & ~overlay;

        if (oldValue == newValue)
        {
            return;
        }

        renderer.Diagnostics.DebugOverlays = newValue;
        RaisePropertyChanged(propertyName);
    }

    public void ToggleDirtyRectsOverlay()
    {
        ShowDirtyRectsOverlay = !ShowDirtyRectsOverlay;
    }

    public void ToggleFpsOverlay()
    {
        ShowFpsOverlay = !ShowFpsOverlay;
    }

    public void ToggleLayoutTimeGraphOverlay()
    {
        ShowLayoutTimeGraphOverlay = !ShowLayoutTimeGraphOverlay;
    }

    public void ToggleRenderTimeGraphOverlay()
    {
        ShowRenderTimeGraphOverlay = !ShowRenderTimeGraphOverlay;
    }

    public void ShowHotKeys()
    {
        SelectView(DevToolsViewKind.Settings);
    }

    public void SelectControl(Control control)
    {
        SelectView(DevToolsViewKind.Elements);
        _selectionCoordinator.SelectControl(control, _elements.CurrentTree);
    }

    public void EnableSnapshotStyles(bool enable)
    {
        if (_elements.CurrentDetails != null)
        {
            _elements.CurrentDetails.SnapshotFrames = enable;
        }
    }

    private void UpdateFocusedControl()
    {
        var element = KeyboardDevice.Instance?.FocusedElement;
        FocusedControl = element?.GetType().Name;
        _currentFocusHighlightAdorner?.Dispose();
        if (FocusHighlighter is IBrush brush
            && element is InputElement input
            && !input.DoesBelongToDevTool()
           )
        {
            _currentFocusHighlightAdorner = ControlHighlightAdorner.Add(input, brush);
        }
    }

    private void KeyboardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KeyboardDevice.Instance.FocusedElement))
        {
            UpdateFocusedControl();
        }
    }

    public void RequestTreeNavigateTo(Control control, bool isVisualTree)
    {
        _selectionCoordinator.RequestTreeNavigateTo(control, isVisualTree);
    }

    [DependsOn(nameof(TreePageViewModel.SelectedNode))]
    [DependsOn(nameof(Content))]
    public bool CanShot(object? parameter)
    {
        return _elements.CurrentTree.SelectedNode != null
            && _elements.CurrentTree.SelectedNode.Model.Target is Visual visual
            && visual.VisualRoot != null;
    }

    public void Shot(object? parameter)
    {
        ShotAsync(parameter).Detach("Failed to capture screenshot for the selected control.");
    }

    private async Task ShotAsync(object? parameter)
    {
        if (_elements.CurrentTree.SelectedNode?.Model.Target is Control control
            && _screenshotHandler is not null)
        {
            await _screenshotHandler.Take(control);
        }
    }

    public void SetOptions(DevToolsOptions options)
    {
        _screenshotHandler = options.ScreenshotHandler;
        StartupScreenIndex = options.StartupScreenIndex;
        ShowImplementedInterfaces = options.ShowImplementedInterfaces;
        FocusHighlighter = options.FocusHighlighterBrush;
        SelectView(options.LaunchView);
    }

    public void ToggleShowImplementedInterfaces(object parameter)
    {
        ShowImplementedInterfaces = !ShowImplementedInterfaces;
        _elements.CurrentTree.UpdatePropertiesView();
    }

    public void ToggleShowDetailsPropertyType(object parameter)
    {
        ShowDetailsPropertyType = !ShowDetailsPropertyType;
    }

    public void SelectFocusHighlighter(object parameter)
    {
        FocusHighlighter = parameter as IBrush;
    }

    public void SelectView(object? parameter)
    {
        if (parameter is DevToolsViewKind viewKind)
        {
            SelectView(viewKind);
        }
    }

    private void SelectView(DevToolsViewKind viewKind)
    {
        Content = viewKind switch
        {
            DevToolsViewKind.Events => _events,
            DevToolsViewKind.Trace => _trace,
            DevToolsViewKind.Settings => _settings,
            _ => _elements,
        };
        SelectedViewKind = viewKind is DevToolsViewKind.Events or DevToolsViewKind.Trace or DevToolsViewKind.Settings
            ? viewKind
            : DevToolsViewKind.Elements;
        RaiseTabSelectionChanged();
    }

    private void SelectTreeFromCoordinator(bool isVisualTree)
    {
        SelectView(DevToolsViewKind.Elements);
        _elements.SelectTreeMode(isVisualTree ? ElementsTreeMode.Visual : ElementsTreeMode.Logical);
    }

    private void RaiseTabSelectionChanged()
    {
        RaisePropertyChanged(nameof(IsElementsSelected));
        RaisePropertyChanged(nameof(IsEventsSelected));
        RaisePropertyChanged(nameof(IsTraceSelected));
        RaisePropertyChanged(nameof(IsSettingsSelected));

        foreach (var tab in Tabs)
        {
            tab.RaiseSelectionChanged();
        }
    }
}
