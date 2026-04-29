<div align="center">

<h1>ClassicDiagnostics.Avalonia</h1>

**Bringing the classic F12 DevTools back to Avalonia 12+**

<p align="center">
  <a href="https://www.nuget.org/packages/ClassicDiagnostics.Avalonia"><img src="https://img.shields.io/nuget/v/ClassicDiagnostics.Avalonia.svg?style=flat-square" alt="NuGet"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" alt="License"></a>
</p>

</div>

<br/>

## 👋 Introduction

With the release of Avalonia 12, the beloved open-source F12 DevTools (`Avalonia.Diagnostics`) has been retired and replaced by the highly advanced, commercial [Avalonia Accelerate](https://avaloniaui.net/Accelerate) suite. 

While the new Accelerate tools are incredibly powerful and represent the future of Avalonia development, we recognize that some developers and small projects still rely on the classic, lightweight, and offline DevTools for basic UI profiling. `ClassicDiagnostics.Avalonia` is a community-maintained migration of the Avalonia 11 `Avalonia.Diagnostics` codebase, adapted to run seamlessly on Avalonia 12. 

Our goal is to provide a smooth transition for developers upgrading to Avalonia 12, while exploring the addition of small, practical utilities in the future.

## 🤝 Community & Commitments

As an open-source project building upon the incredible legacy of the Avalonia team, we want to be fully transparent about our scope and intentions. We maintain a humble stance and respect the official ecosystem:

1. **Support Avalonia Accelerate**: We highly respect the core Avalonia team and their business model. If you or your company have the capacity, **we strongly encourage you to subscribe to Avalonia Accelerate**. It offers a far superior, modern developer experience and directly funds the framework we all love.
2. **Accelerate Banner intact**: The banner promoting Avalonia Accelerate within the classic DevTools will **not** be removed. We believe it is fair and necessary to help promote the official tool.
3. **No Remote Dev Protocols**: We will not develop, maintain, or reverse-engineer remote development protocols or features. Our focus is strictly bounded to the local, classic F12 DevTools experience.

## 🚀 Getting Started

### 1. Install the NuGet package

You can install the latest version using the .NET CLI:

```bash
dotnet add package ClassicDiagnostics.Avalonia
```

or use the NuGet Package Manager in your IDE.

### 2. Attach the DevTools

To use `ClassicDiagnostics.Avalonia`, simply ensure you have the correct namespace and call the attach method in your window initialization:

```csharp
using Avalonia.Controls;
using ClassicDiagnostics.Avalonia; // Note the namespace change!

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

#if DEBUG
        // Attach the classic DevTools
        this.AttachDevTools();
#endif
    }
}
```

## ❤️ Acknowledgements

This project is entirely made possible by the rich legacy of the **Avalonia UI** team and its contributors. We are deeply grateful for their years of effort in maintaining the original `Avalonia.Diagnostics`. 

## 📄 License

This project is licensed under the [MIT License](LICENSE), continuing the open-source spirit of the original codebase.
