namespace ClassicDiagnostics.Avalonia.ViewModels;

internal sealed class StyleClassViewModel(string name, ControlDetailsViewModel owner) : ViewModelBase
{
    public string Name { get; } = name;

    private ControlDetailsViewModel Owner { get; } = owner;

    public void Remove()
    {
        Owner.RemoveClass(this);
    }
}
