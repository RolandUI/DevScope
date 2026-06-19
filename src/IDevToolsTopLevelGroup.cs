using Avalonia.Controls.ApplicationLifetimes;

namespace ClassicDiagnostics.Avalonia;

internal interface IDevToolsTopLevelGroup
{
    IReadOnlyList<TopLevel> Items { get; }
}

internal class ClassicDesktopStyleApplicationLifetimeTopLevelGroup(IClassicDesktopStyleApplicationLifetime lifetime) : IDevToolsTopLevelGroup
{
    private readonly IClassicDesktopStyleApplicationLifetime _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

    public IReadOnlyList<TopLevel> Items => _lifetime.Windows;

    public override int GetHashCode()
    {
        return _lifetime.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is ClassicDesktopStyleApplicationLifetimeTopLevelGroup g && g._lifetime == _lifetime;
    }
}

internal class SingleViewTopLevelGroup(TopLevel topLevel) : IDevToolsTopLevelGroup
{
    private readonly TopLevel _topLevel = topLevel;

    public IReadOnlyList<TopLevel> Items { get; } = [topLevel ?? throw new ArgumentNullException(nameof(topLevel))];

    public override int GetHashCode()
    {
        return _topLevel.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is SingleViewTopLevelGroup g && g._topLevel == _topLevel;
    }
}