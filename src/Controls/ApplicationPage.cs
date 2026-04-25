using Avalonia.Rendering;
using Avalonia.Controls.Templates;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;

namespace ClassicDiagnostics.Avalonia.Controls
{
    internal class ApplicationPage : TopLevelGroup, ICloseable, IDisposable
    {
        private readonly Application application;

        public event EventHandler? Closed;

        public static readonly StyledProperty<ThemeVariant?> RequestedThemeVariantProperty =
            ThemeVariantScope.RequestedThemeVariantProperty.AddOwner<ApplicationPage>();

        public ApplicationPage(ClassicDesktopStyleApplicationLifetimeTopLevelGroup group, Application application)
            : base(group)
        {
            this.application = application;

            if (this.application.ApplicationLifetime is Lifetimes.IControlledApplicationLifetime controller)
            {
                EventHandler<Lifetimes.ControlledApplicationLifetimeExitEventArgs> eh = default!;
                eh = (s, e) =>
                {
                    controller.Exit -= eh;
                    Closed?.Invoke(s, e);
                };
                controller.Exit += eh;
            }
            RendererRoot = application.ApplicationLifetime switch
            {
                Lifetimes.IClassicDesktopStyleApplicationLifetime classic => classic.MainWindow?.Renderer,
                // Lifetimes.ISingleViewApplicationLifetime single => single.MainView?.VisualRoot?.Renderer,
                _ => null
            };

            SetCurrentValue(RequestedThemeVariantProperty, application.RequestedThemeVariant);
            this.application.PropertyChanged += ApplicationPageOnPropertyChanged;
        }

        internal Application Instance => application;

        /// <summary>
        /// Defines the <see cref="DataContext"/> property.
        /// </summary>
        public object? DataContext => application.DataContext;

        /// <summary>
        /// Gets or sets the application's global data templates.
        /// </summary>
        /// <value>
        /// The application's global data templates.
        /// </value>
        public DataTemplates DataTemplates => application.DataTemplates;

        /// <summary>
        /// Gets the application's input manager.
        /// </summary>
        /// <value>
        /// The application's input manager.
        /// </value>
        public InputManager? InputManager => application.InputManager;

        /// <summary>
        /// Gets the application's global resource dictionary.
        /// </summary>
        public IResourceDictionary Resources => application.Resources;

        /// <summary>
        /// Gets the application's global styles.
        /// </summary>
        /// <value>
        /// The application's global styles.
        /// </value>
        /// <remarks>
        /// Global styles apply to all windows in the application.
        /// </remarks>
        public Styles Styles => application.Styles;

        /// <summary>
        /// Application lifetime, use it for things like setting the main window and exiting the app from code
        /// Currently supported lifetimes are:
        /// - <see cref="Lifetimes.IClassicDesktopStyleApplicationLifetime"/>
        /// - <see cref="Lifetimes.ISingleViewApplicationLifetime"/>
        /// - <see cref="Lifetimes.IControlledApplicationLifetime"/> 
        /// </summary>
        public Lifetimes.IApplicationLifetime? ApplicationLifetime => application.ApplicationLifetime;

        /// <summary>
        /// Application name to be used for various platform-specific purposes
        /// </summary>
        public string? Name => application.Name;

        /// <summary>
        /// Gets the root of the visual tree, if the control is attached to a visual tree.
        /// </summary>
        internal IRenderer? RendererRoot { get; }

        /// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariant" />
        public ThemeVariant? RequestedThemeVariant
        {
            get => GetValue(RequestedThemeVariantProperty);
            set => SetValue(RequestedThemeVariantProperty, value);
        }

        public void Dispose()
        {
            application.PropertyChanged -= ApplicationPageOnPropertyChanged;
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
                application.RequestedThemeVariant = change.GetNewValue<ThemeVariant>();
            }
        }
    }
}