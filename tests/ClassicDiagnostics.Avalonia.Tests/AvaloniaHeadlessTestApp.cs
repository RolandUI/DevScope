using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;
using ClassicDiagnostics.Avalonia.Elements;
using ClassicDiagnostics.Avalonia.Elements.Properties.Models;
using ClassicDiagnostics.Avalonia.Elements.Properties.Services;
using ClassicDiagnostics.Avalonia.Elements.Properties.ViewModels;
using ClassicDiagnostics.Avalonia.Elements.Trees;
using ClassicDiagnostics.Avalonia.Rooting;
using ClassicDiagnostics.Avalonia.Shell;

namespace ClassicDiagnostics.Avalonia.Tests;

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
