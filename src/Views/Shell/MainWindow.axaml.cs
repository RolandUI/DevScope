using System.ComponentModel;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using RolandUI.DevScope.Shell;

namespace RolandUI.DevScope.Views.Shell;

internal partial class MainWindow : Window, IStyleHost
{
    private readonly HashSet<Popup> _frozenPopupStates;
    private readonly IDisposable? _inputSubscription;
    private HotKeyConfiguration? _hotKeys;
    private PixelPoint _lastPointerPosition;
    private AvaloniaObject? _root;

    private MainViewModel? ViewModel { get; set; }

    public MainWindow()
    {
        InitializeComponent();

        // Apply the SimpleTheme.Window theme; this must be done after the XAML is parsed as
        // the theme is included in the MainWindow's XAML.
        if (Theme is null && this.FindResource(typeof(Window)) is ControlTheme windowTheme)
            Theme = windowTheme;

        _inputSubscription = InputManager.Instance?.Process
            .Subscribe(x =>
            {
                switch (x)
                {
                    case RawPointerEventArgs { Root: PresentationSource source } pointerEventArgs:
                    {
                        _lastPointerPosition = AvaloniaMutatedApiAccessor.PointToScreen(source, pointerEventArgs.Position) ?? default;
                        break;
                    }
                    case RawKeyEventArgs { Type: RawKeyEventType.KeyDown } keyEventArgs:
                    {
                        RawKeyDown(keyEventArgs);
                        break;
                    }
                }
            });

        _frozenPopupStates = [];

        EventHandler? handleWindowOpened = null;
        handleWindowOpened = delegate
        {
            Opened -= handleWindowOpened;
            if (ViewModel?.StartupScreenIndex is { } index)
            {
                var screens = Screens;
                if (index > -1 && index < screens.ScreenCount)
                {
                    var screen = screens.All[index];
                    Position = screen.Bounds.TopLeft;
                    WindowState = WindowState.Maximized;
                }
            }
        };
        Opened += handleWindowOpened;
    }

    public AvaloniaObject? Root
    {
        get => _root;
        set
        {
            if (_root != value)
            {
                if (_root is ICloseable oldClosable)
                {
                    oldClosable.Closed -= HandleRootClosed;
                }

                _root = value;

                if (_root is ICloseable newClosable)
                {
                    newClosable.Closed += HandleRootClosed;
                    ViewModel = new MainViewModel(_root);
                    DataContext = ViewModel;
                }
                else
                {
                    ViewModel = null;
                    DataContext = null;
                }
            }
        }
    }

    IStyleHost? IStyleHost.StylingParent => null;

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _inputSubscription?.Dispose();

        foreach (var state in _frozenPopupStates)
        {
            state.Closing -= HandlePopupClosing;
        }

        _frozenPopupStates.Clear();

        if (_root is ICloseable cloneable)
        {
            cloneable.Closed -= HandleRootClosed;
            _root = null;
        }

        ViewModel?.Dispose();
        ViewModel = null;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private Control? GetHoveredControl(TopLevel topLevel)
    {
        var point = topLevel.PointToClient(_lastPointerPosition);

        return (Control?)topLevel.GetVisualsAt(
                point,
                x =>
                {
                    if (x is AdornerLayer || !x.IsVisible)
                    {
                        return false;
                    }

                    return x is not IInputElement inputElement || inputElement.IsHitTestVisible;
                })
            .FirstOrDefault();
    }

    private static List<PopupRoot> GetPopupRoots(TopLevel root)
    {
        var popupRoots = new List<PopupRoot>();

        void ProcessProperty<T>(Control control, AvaloniaProperty<T> property)
        {
            if (control.GetValue(property) is IPopupHostProvider popupProvider
                && popupProvider.PopupHost is PopupRoot popupRoot)
            {
                popupRoots.Add(popupRoot);
            }
        }

        foreach (var control in root.GetVisualDescendants().OfType<Control>())
        {
            if (control is Popup popup && popup.Host is PopupRoot popupRoot)
            {
                popupRoots.Add(popupRoot);
            }

            ProcessProperty(control, ContextFlyoutProperty);
            ProcessProperty(control, ContextMenuProperty);
            ProcessProperty(control, FlyoutBase.AttachedFlyoutProperty);
            ProcessProperty(control, ToolTipDiagnostics.ToolTipProperty);
            ProcessProperty(control, Button.FlyoutProperty);
        }

        return popupRoots;
    }

    private void RawKeyDown(RawKeyEventArgs e)
    {
        if (_hotKeys is null || ViewModel is not { PointerOverRoot: TopLevel root } mainViewModel)
        {
            return;
        }

        if (root is PopupRoot popupRoot)
        {
            root = popupRoot.ParentTopLevel;
        }

        var modifiers = MergeModifiers(e.Key, e.Modifiers.ToKeyModifiers());
        if (IsMatched(_hotKeys.ValueFramesFreeze, e.Key, modifiers))
        {
            FreezeValueFrames(mainViewModel);
        }
        else if (IsMatched(_hotKeys.ValueFramesUnfreeze, e.Key, modifiers))
        {
            UnfreezeValueFrames(mainViewModel);
        }
        else if (IsMatched(_hotKeys.TogglePopupFreeze, e.Key, modifiers))
        {
            ToggleFreezePopups(root, mainViewModel);
        }
        else if (IsMatched(_hotKeys.ScreenshotSelectedControl, e.Key, modifiers))
        {
            ScreenshotSelectedControl(mainViewModel);
        }
        else if (IsMatched(_hotKeys.InspectHoveredControl, e.Key, modifiers))
        {
            InspectHoveredControl(root, mainViewModel);
        }

        static bool IsMatched(KeyGesture gesture, Key key, KeyModifiers modifiers)
        {
            return (gesture.Key == key || gesture.Key == Key.None) && modifiers.HasAllFlags(gesture.KeyModifiers);
        }

        // When Control, Shift, or Alt are initially pressed, they are the Key and not part of Modifiers
        // This merges so modifier keys alone can more easily trigger actions
        static KeyModifiers MergeModifiers(Key key, KeyModifiers modifiers)
        {
            return key switch
            {
                Key.LeftCtrl or Key.RightCtrl => modifiers | KeyModifiers.Control,
                Key.LeftShift or Key.RightShift => modifiers | KeyModifiers.Shift,
                Key.LeftAlt or Key.RightAlt => modifiers | KeyModifiers.Alt,
                _ => modifiers,
            };
        }
    }

    private static void FreezeValueFrames(MainViewModel mainViewModel)
    {
        mainViewModel.EnableSnapshotStyles(true);
    }

    private static void UnfreezeValueFrames(MainViewModel mainViewModel)
    {
        mainViewModel.EnableSnapshotStyles(false);
    }

    private void ToggleFreezePopups(TopLevel root, MainViewModel mainViewModel)
    {
        mainViewModel.FreezePopups = !mainViewModel.FreezePopups;

        foreach (var popupRoot in GetPopupRoots(root))
        {
            if (popupRoot.Parent is Popup popup)
            {
                if (mainViewModel.FreezePopups)
                {
                    popup.Closing += HandlePopupClosing;
                    _frozenPopupStates.Add(popup);
                }
                else
                {
                    popup.Closing -= HandlePopupClosing;
                    _frozenPopupStates.Remove(popup);
                }
            }
        }
    }

    private static void ScreenshotSelectedControl(MainViewModel mainViewModel)
    {
        mainViewModel.Shot(null);
    }

    private void InspectHoveredControl(TopLevel root, MainViewModel mainViewModel)
    {
        Control? control = null;

        foreach (var popupRoot in GetPopupRoots(root))
        {
            control = GetHoveredControl(popupRoot);

            if (control != null)
            {
                break;
            }
        }

        control ??= GetHoveredControl(root);

        if (control != null)
        {
            mainViewModel.SelectControl(control);
        }
    }

    private void HandlePopupClosing(object? sender, CancelEventArgs e)
    {
        if (ViewModel?.FreezePopups == true)
        {
            e.Cancel = true;
        }
    }

    private void HandleRootClosed(object? sender, EventArgs e)
    {
        Close();
    }

    public void SetOptions(DevToolsOptions options)
    {
        _hotKeys = options.HotKeys;

        ViewModel?.SetOptions(options);
        if (options.ThemeVariant is { } themeVariant)
        {
            RequestedThemeVariant = themeVariant;
        }
    }

    internal void SelectedControl(Control? control)
    {
        if (control is not null)
        {
            ViewModel?.SelectControl(control);
        }
    }
}