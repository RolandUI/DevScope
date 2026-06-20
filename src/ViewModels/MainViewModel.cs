using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Rendering;
using Avalonia.Threading;
using ClassicDiagnostics.Avalonia.Controls;
using ClassicDiagnostics.Avalonia.Models;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class MainViewModel : ReactiveViewModelBase
{
    private readonly EventsPageViewModel _events;
    private readonly HotKeyPageViewModel _hotKeys;
    private readonly TreePageViewModel _logicalTree;
    private readonly HashSet<string> _pinnedProperties = new();
    private readonly AvaloniaObject _root;
    private readonly TreePageViewModel _visualTree;
    private IDisposable? _currentFocusHighlightAdorner;
    private IScreenshotHandler? _screenshotHandler;

    public MainViewModel(AvaloniaObject root)
    {
        _root = root;
        _logicalTree = new TreePageViewModel(this, LogicalTreeNode.Create(root), _pinnedProperties);
        _visualTree = new TreePageViewModel(this, VisualTreeNode.Create(root), _pinnedProperties);
        _events = new EventsPageViewModel(this);
        _hotKeys = new HotKeyPageViewModel();

        UpdateFocusedControl();

        var keyboard = KeyboardDevice.Instance;
        if (keyboard is not null)
        {
            keyboard.PropertyChanged += KeyboardPropertyChanged;
            Disposable.Create(() => keyboard.PropertyChanged -= KeyboardPropertyChanged)
                .AddTo(LifetimeDisposables);
        }

        SelectedTab = 0;
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
        private set
        {
            if (field is TreePageViewModel oldTree &&
                value is TreePageViewModel newTree &&
                oldTree?.SelectedNode?.Visual is Control control)
            {
                // HACK: We want to select the currently selected control in the new tree, but
                // to select nested nodes in TreeView, currently the TreeView has to be able to
                // expand the parent nodes. Because at this point the TreeView isn't visible,
                // this will fail unless we schedule the selection to run after layout.
                DispatcherTimer.RunOnce(
                    () =>
                    {
                        try
                        {
                            newTree.SelectControl(control);
                        }
                        catch (Exception exception)
                        {
                            DevToolsDiagnostics.Report(
                                exception,
                                "Failed to select the previously selected control after switching tree tabs.");
                        }
                    },
                    TimeSpan.FromMilliseconds(0));
            }

            SetProperty(ref field, value);
        }
    }

    public int SelectedTab
    {
        get;
        // [MemberNotNull(nameof(_content))]
        set
        {
            field = value;

            switch (value)
            {
                case 1:
                    Content = _visualTree;
                    break;
                case 2:
                    Content = _events;
                    break;
                case 3:
                    Content = _hotKeys;
                    break;
                default:
                    Content = _logicalTree;
                    break;
            }

            RaisePropertyChanged();
        }
    }

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
        SelectedTab = 3;
    }

    public void SelectControl(Control control)
    {
        var tree = Content as TreePageViewModel;

        tree?.SelectControl(control);
    }

    public void EnableSnapshotStyles(bool enable)
    {
        if (Content is TreePageViewModel treeVm && treeVm.Details != null)
        {
            treeVm.Details.SnapshotFrames = enable;
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
        var tree = isVisualTree ? _visualTree : _logicalTree;

        var node = tree.FindNode(control);

        if (node != null)
        {
            SelectedTab = isVisualTree ? 1 : 0;

            tree.SelectControl(control);
        }
    }

    [DependsOn(nameof(TreePageViewModel.SelectedNode))]
    [DependsOn(nameof(Content))]
    public bool CanShot(object? parameter)
    {
        return Content is TreePageViewModel tree
            && tree.SelectedNode != null
            && tree.SelectedNode.Visual is Visual visual
            && visual.VisualRoot != null;
    }

    public void Shot(object? parameter)
    {
        ShotAsync(parameter).Detach("Failed to capture screenshot for the selected control.");
    }

    private async Task ShotAsync(object? parameter)
    {
        if ((Content as TreePageViewModel)?.SelectedNode?.Visual is Control control
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
        SelectedTab = (int)options.LaunchView;

        _hotKeys.SetOptions(options);
    }

    public void ToggleShowImplementedInterfaces(object parameter)
    {
        ShowImplementedInterfaces = !ShowImplementedInterfaces;
        if (Content is TreePageViewModel viewModel)
        {
            viewModel.UpdatePropertiesView();
        }
    }

    public void ToggleShowDetailsPropertyType(object parameter)
    {
        ShowDetailsPropertyType = !ShowDetailsPropertyType;
    }

    public void SelectFocusHighlighter(object parameter)
    {
        FocusHighlighter = parameter as IBrush;
    }
}
