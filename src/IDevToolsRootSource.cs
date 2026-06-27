using Avalonia.Controls.ApplicationLifetimes;
using Application = Avalonia.Application;

namespace ClassicDiagnostics.Avalonia;

internal interface IDevToolsRootSource
{
    IReadOnlyList<TopLevel> Items { get; }
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

internal sealed class SingleViewApplicationRootSource(
    Application application,
    ISingleViewApplicationLifetime lifetime
) : IDevToolsRootSource
{
    private readonly Application _application = application ?? throw new ArgumentNullException(nameof(application));
    private readonly ISingleViewApplicationLifetime _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

    public IReadOnlyList<TopLevel> Items
    {
        get
        {
            return _lifetime switch
            {
                // Browser/iOS single-view lifetimes expose the actual host through the
                // private-api ISingleTopLevelApplicationLifetime. Prefer it as the presentation
                // root. MainView is only a clue for finding its TopLevel, not an Application child.
                ISingleTopLevelApplicationLifetime { TopLevel: { } topLevel } => [topLevel],
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
