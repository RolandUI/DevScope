using Avalonia.Controls.ApplicationLifetimes;
using Application = Avalonia.Application;

namespace RolandUI.DevScope;

internal enum DevToolsHostKind
{
    DesktopWindow,
    EmbeddedSingleView,
}

internal interface IDevToolsRootSource
{
    DevToolsHostKind HostKind { get; }

    IReadOnlyList<TopLevel> Items { get; }
}

internal interface IDevToolsSingleViewLifetime
{
    Control? MainView { get; set; }

    TopLevel? TopLevel { get; }
}

internal sealed class DevToolsSingleViewLifetime(ISingleViewApplicationLifetime lifetime) : IDevToolsSingleViewLifetime
{
    private readonly ISingleViewApplicationLifetime _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

    public Control? MainView
    {
        get => _lifetime.MainView;
        set => _lifetime.MainView = value;
    }

    public TopLevel? TopLevel => (_lifetime as ISingleTopLevelApplicationLifetime)?.TopLevel;
}

internal static class DevToolsRootSources
{
    public static bool TryCreate(Application? application, out IDevToolsRootSource? source)
    {
        switch (application?.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime classic:
                source = new ClassicDesktopApplicationRootSource(application, classic);
                return true;
            case ISingleViewApplicationLifetime singleView:
                source = new SingleViewApplicationRootSource(application, singleView);
                return true;
            default:
                source = null;
                return false;
        }
    }

    public static IDevToolsRootSource Create(Application application)
    {
        return application.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime classic => new ClassicDesktopApplicationRootSource(application, classic),
            ISingleViewApplicationLifetime singleView => new SingleViewApplicationRootSource(application, singleView),
            _ => throw new ArgumentException(
                "DevTools can only attach to applications that expose classic desktop or single-view lifetimes.",
                nameof(application)),
        };
    }
}

internal sealed class ClassicDesktopApplicationRootSource(
    Application application,
    IClassicDesktopStyleApplicationLifetime lifetime
) : IDevToolsRootSource
{
    private readonly Application _application = application ?? throw new ArgumentNullException(nameof(application));
    private readonly IClassicDesktopStyleApplicationLifetime _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

    public DevToolsHostKind HostKind => DevToolsHostKind.DesktopWindow;

    public IReadOnlyList<TopLevel> Items => _lifetime.Windows;

    public override int GetHashCode()
    {
        return _application.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is ClassicDesktopApplicationRootSource source &&
            ReferenceEquals(source._application, _application);
    }
}

internal sealed class SingleViewApplicationRootSource : IDevToolsRootSource
{
    private readonly Application _application;
    private readonly IDevToolsSingleViewLifetime _lifetime;

    public SingleViewApplicationRootSource(Application application, ISingleViewApplicationLifetime lifetime)
        : this(application, new DevToolsSingleViewLifetime(lifetime))
    {
    }

    internal SingleViewApplicationRootSource(Application application, IDevToolsSingleViewLifetime lifetime)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    public DevToolsHostKind HostKind => DevToolsHostKind.EmbeddedSingleView;

    internal IDevToolsSingleViewLifetime Lifetime => _lifetime;

    public IReadOnlyList<TopLevel> Items
    {
        get
        {
            return _lifetime switch
            {
                { TopLevel: { } topLevel } => [topLevel],
                { MainView: { } mainView } when TopLevel.GetTopLevel(mainView) is { } mainViewTopLevel => [mainViewTopLevel],
                _ => []
            };
        }
    }

    public override int GetHashCode()
    {
        return _application.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is SingleViewApplicationRootSource source &&
            ReferenceEquals(source._application, _application);
    }
}

internal sealed class SingleTopLevelRootSource(TopLevel topLevel) : IDevToolsRootSource
{
    public TopLevel TopLevel { get; } = topLevel ?? throw new ArgumentNullException(nameof(topLevel));

    public DevToolsHostKind HostKind => DevToolsHostKind.DesktopWindow;

    public IReadOnlyList<TopLevel> Items { get; } = [topLevel];

    public override int GetHashCode()
    {
        return TopLevel.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is SingleTopLevelRootSource source && ReferenceEquals(source.TopLevel, TopLevel);
    }
}
