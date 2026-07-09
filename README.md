<div align="center">

<h1>DevScope</h1>

**Local F12 diagnostics compatible with Avalonia 12**

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" alt="License"></a>
  <a href="https://github.com/RolandUI/DevScope/issues"><img src="https://img.shields.io/github/issues/RolandUI/DevScope?style=flat-square" alt="GitHub Issues"></a>
</p>

</div>

<br/>

## 👋 Introduction

With the release of Avalonia 12, the open-source F12 DevTools ([Avalonia.Diagnostics](https://github.com/AvaloniaUI/Avalonia.Diagnostics)) was retired and replaced by the commercial [Avalonia Accelerate](https://avaloniaui.net/Accelerate) suite.

`DevScope` continues the work of [Sylinko/ClassicDiagnostics.Avalonia](https://github.com/Sylinko/ClassicDiagnostics.Avalonia), a community migration of the Avalonia 11 `Avalonia.Diagnostics` codebase to Avalonia 12. DevScope is an independent continuation focused on preserving the classic, lightweight, local, and offline diagnostics workflow.

Our goal is to provide a smooth transition for developers upgrading to Avalonia 12, while exploring the addition of small, practical utilities in the future.

## 🤝 Community & Commitments

As an open-source project building upon the incredible legacy of the Avalonia team, we want to be fully transparent about our scope and intentions. We maintain a humble stance and respect the official ecosystem:

1. **Support Avalonia Accelerate**: We highly respect the core Avalonia team and their business model. If you or your company have the capacity, **we strongly encourage you to subscribe to Avalonia Accelerate**. It offers a far superior, modern developer experience and directly funds the framework we all love.
2. **Accelerate Banner intact**: The banner promoting Avalonia Accelerate within the classic DevTools will **not** be removed. We believe it is fair and necessary to help promote the official tool.
3. **No Remote Dev Protocols**: We will not develop, maintain, or reverse-engineer remote development protocols or features. Our focus is strictly bounded to the local, classic F12 DevTools experience.

## 🚀 Getting Started

### 1. Install the NuGet package

The package ID is `RolandUI.DevScope`. After the first NuGet release, install it with:

```bash
dotnet add package RolandUI.DevScope --prerelease
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

        base.OnFrameworkInitializationCompleted();

#if DEBUG
        _devToolsSession = this.AttachDevTools();
#endif
    }
}
```

Keeping the returned `IDisposable` allows the application to detach the F12 input subscription explicitly when required.

## Current Scope

`DevScope` is focused on local, classic F12 diagnostics for Avalonia 12+ applications. The current source targets Avalonia 12.1 and supports both `net8.0` and `net10.0` consumers.

The current preview provides a single global desktop DevTools window, logical and visual tree inspection, routed-event monitoring, property inspection and editing, collection navigation, style-class and pseudo-class editing, flags-enum editing, diagnostic overlays, hotkeys, and screenshots.

The project is intentionally lightweight: it does not add a remote debugging service, does not replace Avalonia Accelerate, and does not try to become a full external diagnostics platform.

## Known Limitations

- The DevTools surface currently opens as a desktop `Window`. Single-view roots can be discovered, but Browser, Android, and iOS still need an embedded host ([#3](https://github.com/RolandUI/DevScope/issues/3)).
- Arrays, lists, dictionaries, and other enumerable values can be inspected and navigated, but their child items are currently read-only ([#1](https://github.com/RolandUI/DevScope/issues/1)).
- The Trace tab is present but does not capture or display trace entries yet ([#2](https://github.com/RolandUI/DevScope/issues/2)).
- Diagnostic animation-clock controls are not implemented and require version-sensitive Avalonia internal APIs ([#4](https://github.com/RolandUI/DevScope/issues/4)).

## Roadmap

Planned work is tracked in [GitHub Issues](https://github.com/RolandUI/DevScope/issues). Remote development protocols remain intentionally out of scope.

## Releasing

Maintainers should follow the [DevScope Release Guide](docs/RELEASING.md). It defines the release gate, version and tag rules, GitHub Release workflow, NuGet verification, and recovery procedure.

## ❤️ Acknowledgements

DevScope builds on the work of two open-source projects:

- [Avalonia.Diagnostics](https://github.com/AvaloniaUI/Avalonia.Diagnostics), the original F12 DevTools maintained by the Avalonia UI team and contributors.
- [ClassicDiagnostics.Avalonia](https://github.com/Sylinko/ClassicDiagnostics.Avalonia), Sylinko's Avalonia 12 migration that DevScope directly continues.

We are grateful to both projects and their contributors. DevScope is an independent community project and is not affiliated with, sponsored by, or endorsed by the Avalonia UI project. Avalonia and Avalonia Accelerate are trademarks of their respective owners.

## 📄 License

This project is licensed under the [MIT License](LICENSE), continuing the open-source spirit of the original codebase.
