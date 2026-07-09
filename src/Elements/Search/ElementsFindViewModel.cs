using RolandUI.DevScope.Elements.Trees;
using RolandUI.DevScope.ViewModels;

namespace RolandUI.DevScope.Elements.Search;

internal sealed class ElementsFindViewModel : ViewModelBase
{
    public bool HasMatches => MatchCount > 0;

    public bool IsVisible
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value)
            {
                Refresh();
            }
        }
    }

    public int CurrentIndex
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RaisePropertyChanged(nameof(StatusText));
            }
        }
    }

    public int MatchCount
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RaisePropertyChanged(nameof(HasMatches));
                RaisePropertyChanged(nameof(StatusText));
            }
        }
    }

    public string Query
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Refresh();
            }
        }
    } = string.Empty;

    public string StatusText
    {
        get
        {
            if (!string.IsNullOrEmpty(_deferredMessage))
            {
                return _deferredMessage;
            }

            if (string.IsNullOrWhiteSpace(Query))
            {
                return string.Empty;
            }

            return MatchCount == 0 ? "No matches" : $"{CurrentIndex + 1} / {MatchCount}";
        }
    }

    public bool UseCaseSensitiveFilter
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Refresh();
            }
        }
    }

    public bool UseRegexFilter
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Refresh();
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
                Refresh();
            }
        }
    }

    private IReadOnlyList<TreeNodeViewModel> _matches = [];
    private string? _deferredMessage;
    private ElementsTreeViewModel? _tree;

    public void AttachTree(ElementsTreeViewModel tree)
    {
        _tree = tree;
        Refresh();
    }

    public void FindNext()
    {
        Move(1);
    }

    public void FindPrevious()
    {
        Move(-1);
    }

    private void Move(int offset)
    {
        if (_matches.Count == 0 || _tree is null)
        {
            return;
        }

        var nextIndex = (CurrentIndex + offset + _matches.Count) % _matches.Count;
        CurrentIndex = nextIndex;
        _tree.SelectAndRevealNode(_matches[nextIndex]);
    }

    private void Refresh()
    {
        if (_tree is null)
        {
            _matches = [];
            CurrentIndex = 0;
            MatchCount = 0;
            _deferredMessage = null;
            RaisePropertyChanged(nameof(StatusText));
            return;
        }

        var results = TreeSearchService.Search(
            _tree,
            Query,
            new TreeSearchOptions(UseCaseSensitiveFilter, UseRegexFilter, UseWholeWordFilter));
        _matches = results.Matches;
        _deferredMessage = results.DeferredMessage;
        MatchCount = _matches.Count;
        CurrentIndex = 0;

        if (_matches.Count > 0)
        {
            _tree.SelectAndRevealNode(_matches[0]);
        }

        RaisePropertyChanged(nameof(StatusText));
    }
}