using ClassicDiagnostics.Avalonia.ViewModels;

namespace ClassicDiagnostics.Avalonia.Elements.Styles;

internal sealed class StyleClassViewModel(string name, ElementDetailsViewModel owner) : ViewModelBase
{
    public string Name { get; } = name;

    private ElementDetailsViewModel Owner { get; } = owner;

    public void Remove()
    {
        Owner.RemoveClass(this);
    }
}