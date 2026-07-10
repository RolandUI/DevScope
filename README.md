<div align="center">

<h1>DevScope</h1>

**Local F12 diagnostics compatible with Avalonia 12**

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" alt="License"></a>
  <a href="https://github.com/RolandUI/DevScope/issues"><img src="https://img.shields.io/github/issues/RolandUI/DevScope?style=flat-square" alt="GitHub Issues"></a>
</p>

</div>

<br/>

## Introduction

With the release of Avalonia 12, the open-source F12 DevTools ([Avalonia.Diagnostics](https://github.com/AvaloniaUI/Avalonia.Diagnostics)) was retired and replaced by the commercial [Avalonia Accelerate](https://avaloniaui.net/Accelerate) suite.

`DevScope` continues the work of [Sylinko/ClassicDiagnostics.Avalonia](https://github.com/Sylinko/ClassicDiagnostics.Avalonia), a community migration of the Avalonia 11 `Avalonia.Diagnostics` codebase to Avalonia 12. DevScope is an independent continuation focused on preserving the classic, lightweight, local, and offline diagnostics workflow.

Our goal is to provide a smooth transition for developers upgrading to Avalonia 12, while exploring the addition of small, practical utilities in the future.

## Community & Commitments

As an open-source project building upon the incredible legacy of the Avalonia team, we want to be fully transparent about our scope and intentions. We maintain a humble stance and respect the official ecosystem:

1. **Support Avalonia Accelerate**: We highly respect the core Avalonia team and their business model. If you or your company have the capacity, **we strongly encourage you to subscribe to Avalonia Accelerate**. It offers a far superior, modern developer experience and directly funds the framework we all love.
2. **Accelerate Banner intact**: The banner promoting Avalonia Accelerate within the classic DevTools will **not** be removed. We believe it is fair and necessary to help promote the official tool.
3. **No Remote Dev Protocols**: We will not develop, maintain, or reverse-engineer remote development protocols or features. Our focus is strictly bounded to the local, classic F12 DevTools experience.

## Getting Started

### 1. Install the NuGet package

The package ID is `RolandUI.DevScope`. Install the current release with:

```bash
dotnet add package RolandUI.DevScope --version 0.1.0
```

or use the NuGet Package Manager in your IDE.

### 2. Attach the DevTools

Attach the tools at the application level after Avalonia has finished initializing:

```csharp
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using RolandUI.DevScope;

public partial class App : Application
{
    private IDisposable? _devToolsSession;

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new AppView();
        }

        base.OnFrameworkInitializationCompleted();

#if DEBUG
        _devToolsSession = this.AttachDevTools();
#endif
    }
}
```

Keeping the returned `IDisposable` allows the application to detach the F12 input subscription explicitly when required. Repeated `AttachDevTools()` calls for the same `Application` share one session and one input subscription; disposing the final returned handle closes the surface and detaches the session. The options from the first active attachment are used until that shared session is fully detached.

## Current Scope

`DevScope` is focused on local, classic F12 diagnostics for Avalonia 12+ applications. The current source targets Avalonia 12.1 and supports both `net8.0` and `net10.0` consumers.

The current release provides one active DevTools surface per application, logical and visual tree inspection, routed-event monitoring, property inspection and editing, mutable collection item editing, collection navigation, style-class and pseudo-class editing, flags-enum editing, diagnostic overlays, hotkeys, and screenshots.

Classic desktop lifetimes open the existing dedicated DevTools window; pressing the configured gesture again activates that window. `ISingleViewApplicationLifetime` applications instead open the same DevTools `MainView` as a full-size in-app overlay, and repeated activation toggles it closed. DevScope uses the host `OverlayLayer` when available and otherwise temporarily wraps the original `MainView`; closing the overlay or disposing the session restores the original visual tree and releases owned subscriptions.

The embedded host works at the Avalonia single-view lifetime level, including browser and mobile hosts that provide an initialized `MainView`. Activation still depends on the configured key gesture reaching Avalonia's input manager: browsers can reserve F12 for their own tools, and touch-only devices may need a different `DevToolsOptions.Gesture`. DevScope does not add a remote or out-of-process activation channel.

The Trace tab captures in-process [`System.Diagnostics.Trace`](https://learn.microsoft.com/dotnet/api/system.diagnostics.trace) events while a DevScope session is open. It keeps a bounded 1,000-entry buffer and supports live filtering, pause/resume, clear, and optional auto-scroll. Applications can route Avalonia logging into this source with Avalonia's `LogToTrace` startup configuration.

The Trace tab also contains an **experimental animation clock** for Avalonia 12.1. Select an animatable control or subtree in the Elements tab, attach the clock, then pause, resume, advance by a deterministic millisecond step, reset to zero, or detach it. The clock is inherited only by the selected subtree, so DevScope and the rest of the inspected application continue running normally. Detaching DevScope restores the exact previous clock-property value; animations that captured the diagnostic clock continue on a normal pass-through timeline until they finish.

This feature uses the internal Avalonia 12.1 `IClock`, `ClockBase`, and `Animatable.Clock` shape behind one compatibility-checked adapter. If that shape changes, the controls report an unavailable diagnostic instead of attempting the mutation. Avalonia animations capture their clock when they start, so animations that were already running before attachment keep their original clock; start or restart the target animation after attachment to control it.

The project is intentionally lightweight: it does not add a remote debugging service, does not replace Avalonia Accelerate, and does not try to become a full external diagnostics platform.

## Roadmap

Planned work is tracked in [GitHub Issues](https://github.com/RolandUI/DevScope/issues). Remote development protocols remain intentionally out of scope.

## Releasing

Maintainers should follow the [DevScope Release Guide](docs/RELEASING.md). It defines the release gate, version and tag rules, GitHub Release workflow, nuget.org and GitHub Packages verification, and recovery procedure.

## Acknowledgements

DevScope builds on the work of two open-source projects:

- [Avalonia.Diagnostics](https://github.com/AvaloniaUI/Avalonia.Diagnostics), the original F12 DevTools maintained by the Avalonia UI team and contributors.
- [ClassicDiagnostics.Avalonia](https://github.com/Sylinko/ClassicDiagnostics.Avalonia), Sylinko's Avalonia 12 migration that DevScope directly continues.

We are grateful to both projects and their contributors. DevScope is an independent community project and is not affiliated with, sponsored by, or endorsed by the Avalonia UI project. Avalonia and Avalonia Accelerate are trademarks of their respective owners.

## License

This project is licensed under the [MIT License](LICENSE), continuing the open-source spirit of the original codebase.
