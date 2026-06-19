using Avalonia.Controls.Templates;
using Avalonia.Rendering;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;

namespace ClassicDiagnostics.Avalonia.Controls;

internal class ApplicationPage : TopLevelGroup, ICloseable, IDisposable
{
    private readonly EventHandler<Lifetimes.ControlledApplicationLifetimeExitEventArgs>? _controlledExitHandler;
    private readonly Lifetimes.IControlledApplicationLifetime? _controlledLifetime;
    private bool _isDisposed;

    public readonly static StyledProperty<ThemeVariant?> RequestedThemeVariantProperty =
        ThemeVariantScope.RequestedThemeVariantProperty.AddOwner<ApplicationPage>();

    public ApplicationPage(ClassicDesktopStyleApplicationLifetimeTopLevelGroup group, Application application)
        : base(group)
    {
        this.Instance = application;

        if (this.Instance.ApplicationLifetime is Lifetimes.IControlledApplicationLifetime controller)
        {
            _controlledLifetime = controller;
            _controlledExitHandler = (s, e) =>
            {
                Closed?.Invoke(s, e);
            };
            controller.Exit += _controlledExitHandler;
        }
        RendererRoot = application.ApplicationLifetime switch
        {
            Lifetimes.IClassicDesktopStyleApplicationLifetime classic => classic.MainWindow?.Renderer,
            // Lifetimes.ISingleViewApplicationLifetime single => single.MainView?.VisualRoot?.Renderer,
            _ => null,
        };

        SetCurrentValue(RequestedThemeVariantProperty, application.RequestedThemeVariant);
        this.Instance.PropertyChanged += ApplicationPageOnPropertyChanged;
    }

    internal Application Instance { get; }

    /// <summary>
    ///     Defines the <see cref="DataContext" /> property.
    /// </summary>
    public object? DataContext => Instance.DataContext;

    /// <summary>
    ///     Gets or sets the application's global data templates.
    /// </summary>
    /// <value>
    ///     The application's global data templates.
    /// </value>
    public DataTemplates DataTemplates => Instance.DataTemplates;

    /// <summary>
    ///     Gets the application's input manager.
    /// </summary>
    /// <value>
    ///     The application's input manager.
    /// </value>
    public InputManager? InputManager => Instance.InputManager;

    /// <summary>
    ///     Gets the application's global resource dictionary.
    /// </summary>
    public IResourceDictionary Resources => Instance.Resources;

    /// <summary>
    ///     Gets the application's global styles.
    /// </summary>
    /// <value>
    ///     The application's global styles.
    /// </value>
    /// <remarks>
    ///     Global styles apply to all windows in the application.
    /// </remarks>
    public Styles Styles => Instance.Styles;

    /// <summary>
    ///     Application lifetime, use it for things like setting the main window and exiting the app from code
    ///     Currently supported lifetimes are:
    ///     - <see cref="Lifetimes.IClassicDesktopStyleApplicationLifetime" />
    ///     - <see cref="Lifetimes.ISingleViewApplicationLifetime" />
    ///     - <see cref="Lifetimes.IControlledApplicationLifetime" />
    /// </summary>
    public Lifetimes.IApplicationLifetime? ApplicationLifetime => Instance.ApplicationLifetime;

    /// <summary>
    ///     Application name to be used for various platform-specific purposes
    /// </summary>
    public string? Name => Instance.Name;

    /// <summary>
    ///     Gets the root of the visual tree, if the control is attached to a visual tree.
    /// </summary>
    internal IRenderer? RendererRoot { get; }

    /// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariant" />
    public ThemeVariant? RequestedThemeVariant
    {
        get => GetValue(RequestedThemeVariantProperty);
        set => SetValue(RequestedThemeVariantProperty, value);
    }

    public event EventHandler? Closed;

    public override void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Instance.PropertyChanged -= ApplicationPageOnPropertyChanged;

        // The application outlives this synthetic root; keeping Exit subscribed would keep
        // the DevTools view model graph alive after the diagnostics window is closed.
        if (_controlledLifetime is not null && _controlledExitHandler is not null)
        {
            _controlledLifetime.Exit -= _controlledExitHandler;
        }

        base.Dispose();
        _isDisposed = true;
    }

    private void ApplicationPageOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == RequestedThemeVariantProperty)
        {
            SetCurrentValue(RequestedThemeVariantProperty, e.GetNewValue<ThemeVariant>());
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == RequestedThemeVariantProperty)
        {
            Instance.RequestedThemeVariant = change.GetNewValue<ThemeVariant>();
        }
    }
}
