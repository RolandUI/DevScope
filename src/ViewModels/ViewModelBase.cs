using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
    }

    protected bool SetProperty<T>([NotNullIfNotNull("value")] ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        return false;
    }

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        var e = new PropertyChangedEventArgs(propertyName);
        OnPropertyChanged(e);
        PropertyChanged?.Invoke(this, e);
    }
}