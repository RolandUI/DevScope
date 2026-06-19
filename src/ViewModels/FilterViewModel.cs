using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace ClassicDiagnostics.Avalonia.ViewModels;

internal class FilterViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly Dictionary<string, string> _errors = new();
    private Regex? _filterRegex;

    public string FilterString
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateFilterRegex();
                RefreshFilter?.Invoke(this, EventArgs.Empty);
            }
        }
    } = string.Empty;

    public bool UseRegexFilter
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateFilterRegex();
                RefreshFilter?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool UseCaseSensitiveFilter
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateFilterRegex();
                RefreshFilter?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool UseWholeWordFilter
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateFilterRegex();
                RefreshFilter?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool HasErrors => _errors.Count > 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName != null
            && _errors.TryGetValue(propertyName, out var error))
        {
            yield return error;
        }
    }

    public event EventHandler? RefreshFilter;

    public bool Filter(string input)
    {
        return _filterRegex?.IsMatch(input) ?? true;
    }

    private void UpdateFilterRegex()
    {
        void ClearError()
        {
            if (_errors.Remove(nameof(FilterString)))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(FilterString)));
            }
        }

        try
        {
            var options = RegexOptions.Compiled;
            var pattern = UseRegexFilter ? FilterString.Trim() : Regex.Escape(FilterString.Trim());
            if (!UseCaseSensitiveFilter)
            {
                options |= RegexOptions.IgnoreCase;
            }
            if (UseWholeWordFilter)
            {
                pattern = $"\\b(?:{pattern})\\b";
            }

            _filterRegex = new Regex(pattern, options);
            ClearError();
        }
        catch (Exception exception)
        {
            _errors[nameof(FilterString)] = exception.Message;
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(FilterString)));
        }
    }
}