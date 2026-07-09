using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;
using RolandUI.DevScope.Elements;
using RolandUI.DevScope.Elements.Properties.Models;
using RolandUI.DevScope.Elements.Properties.Services;
using RolandUI.DevScope.Elements.Properties.ViewModels;
using RolandUI.DevScope.Elements.Trees;
using RolandUI.DevScope.Rooting;
using RolandUI.DevScope.Shell;

namespace RolandUI.DevScope.Tests;

public static class AvaloniaHeadlessTestApp
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<Application>()
            .AfterSetup(builder =>
            {
                builder.Instance?.Styles.Add(new SimpleTheme());
            })
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
