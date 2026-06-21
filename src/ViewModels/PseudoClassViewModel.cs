namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class PseudoClassViewModel : ViewModelBase
{
    public PseudoClassViewModel(string name, StyledElement source)
    {
        Name = name;
        _source = source;
        _pseudoClasses = _source.Classes;

        Update();
    }

    public string Name { get; }

    public string? Error
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public bool IsActive
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
            {
                return;
            }

            if (!_isUpdating)
            {
                SetPseudoClass(value);
            }
        }
    }

    private readonly IPseudoClasses _pseudoClasses;
    private readonly StyledElement _source;
    private bool _isUpdating;

    public void Update()
    {
        try
        {
            _isUpdating = true;

            IsActive = _source.Classes.Contains(Name);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SetPseudoClass(bool value)
    {
        try
        {
            _pseudoClasses.Set(Name, value);
            Error = null;
        }
        catch (Exception exception)
        {
            Error = exception.Message;
            DevToolsDiagnostics.Report(exception, $"Failed to set pseudo class '{Name}'.");
            Update();
        }
    }
}
