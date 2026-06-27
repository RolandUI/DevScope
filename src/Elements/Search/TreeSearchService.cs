using System.Text.RegularExpressions;
using ClassicDiagnostics.Avalonia.Elements.Trees;

namespace ClassicDiagnostics.Avalonia.Elements.Search;

internal static class TreeSearchService
{
    public static TreeSearchResults Search(ElementsTreeViewModel tree, string? query)
    {
        return Search(tree, query, TreeSearchOptions.Default);
    }

    public static TreeSearchResults Search(ElementsTreeViewModel tree, string? query, TreeSearchOptions options)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (normalizedQuery.Length == 0)
        {
            return TreeSearchResults.Empty;
        }

        if (normalizedQuery.StartsWith('/'))
        {
            return TreeSearchResults.Deferred("XPath-like path search is planned.");
        }

        return LooksLikeSelector(normalizedQuery) && SimpleSelector.TryParse(normalizedQuery, out var selector) ?
            Search(tree, node => selector.Matches(node)) :
            SearchText(tree, normalizedQuery, options);
    }

    private static TreeSearchResults Search(ElementsTreeViewModel tree, Func<TreeNodeViewModel, bool> predicate)
    {
        var matches = new List<TreeNodeViewModel>();

        foreach (var root in tree.Nodes)
        {
            Visit(root, predicate, matches);
        }

        return new TreeSearchResults(matches, null);
    }

    private static TreeSearchResults SearchText(ElementsTreeViewModel tree, string query, TreeSearchOptions options)
    {
        try
        {
            var matcher = TextMatcher.Create(query, options);
            return Search(tree, node => MatchesText(node, matcher));
        }
        catch (ArgumentException exception)
        {
            return TreeSearchResults.Message($"Invalid regex: {exception.Message}");
        }
    }

    private static bool MatchesText(TreeNodeViewModel node, TextMatcher matcher)
    {
        return matcher.IsMatch(node.Type)
            || matcher.IsMatch(node.ElementName)
            || matcher.IsMatch(node.Classes)
            || matcher.IsMatch(GetWindowTitle(node));
    }

    private static string? GetWindowTitle(TreeNodeViewModel node)
    {
        return node.Model.Target is Window window ? window.Title : null;
    }

    private static bool LooksLikeSelector(string query)
    {
        return query[0] is '.' or '#' or ':' || query.Any(c => c is '.' or '#' or ':');
    }

    private static void Visit(
        TreeNodeViewModel node,
        Func<TreeNodeViewModel, bool> predicate,
        ICollection<TreeNodeViewModel> matches)
    {
        if (predicate(node))
        {
            matches.Add(node);
        }

        foreach (var child in node.Children)
        {
            Visit(child, predicate, matches);
        }
    }

    private sealed class SimpleSelector
    {
        private string? Name { get; set; }

        private string? Type { get; set; }

        private readonly List<string> _classes = [];
        private readonly List<string> _pseudoClasses = [];

        public bool Matches(TreeNodeViewModel node)
        {
            return MatchesType(node)
                && MatchesName(node)
                && MatchesClasses(node)
                && MatchesPseudoClasses(node);
        }

        public static bool TryParse(string query, out SimpleSelector selector)
        {
            selector = new SimpleSelector();

            var index = 0;
            if (IsIdentifierStart(query[index]))
            {
                selector.Type = ReadIdentifier(query, ref index);
            }

            while (index < query.Length)
            {
                var marker = query[index++];
                var value = ReadIdentifier(query, ref index);

                if (value.Length == 0)
                {
                    selector = new SimpleSelector();
                    return false;
                }

                switch (marker)
                {
                    case '#':
                        selector.Name = value;
                        break;
                    case '.':
                        selector._classes.Add(value);
                        break;
                    case ':':
                        selector._pseudoClasses.Add(value);
                        break;
                    default:
                        selector = new SimpleSelector();
                        return false;
                }
            }

            return selector.Type is not null
                || selector.Name is not null
                || selector._classes.Count > 0
                || selector._pseudoClasses.Count > 0;
        }

        private bool MatchesClasses(TreeNodeViewModel node)
        {
            return _classes.All(@class => HasClass(node, @class));
        }

        private bool MatchesName(TreeNodeViewModel node)
        {
            return Name is null || string.Equals(node.ElementName, Name, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesPseudoClasses(TreeNodeViewModel node)
        {
            return _pseudoClasses.All(pseudoClass => HasClass(node, $":{pseudoClass}"));
        }

        private bool MatchesType(TreeNodeViewModel node)
        {
            return Type is null || string.Equals(node.Type, Type, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasClass(TreeNodeViewModel node, string @class)
        {
            return node.Model.Target is StyledElement styledElement
                && styledElement.Classes.Any(x => string.Equals(x, @class, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsIdentifierPart(char c)
        {
            return char.IsLetterOrDigit(c) || c is '_' or '-';
        }

        private static string ReadIdentifier(string query, ref int index)
        {
            var start = index;
            while (index < query.Length && IsIdentifierPart(query[index]))
            {
                index++;
            }

            return query[start..index];
        }
    }

    private sealed class TextMatcher
    {
        private readonly Regex _regex;

        private TextMatcher(Regex regex)
        {
            _regex = regex;
        }

        public static TextMatcher Create(string query, TreeSearchOptions options)
        {
            var pattern = options.UseRegex ? query : Regex.Escape(query);
            if (options.UseWholeWord)
            {
                pattern = $@"\b(?:{pattern})\b";
            }

            var regexOptions = RegexOptions.Compiled;
            if (!options.UseCaseSensitive)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            return new TextMatcher(new Regex(pattern, regexOptions));
        }

        public bool IsMatch(string? value)
        {
            return value is not null && _regex.IsMatch(value);
        }
    }
}

internal sealed record TreeSearchOptions(bool UseCaseSensitive, bool UseRegex, bool UseWholeWord)
{
    public static TreeSearchOptions Default { get; } = new(false, false, false);
}

internal sealed record TreeSearchResults(IReadOnlyList<TreeNodeViewModel> Matches, string? DeferredMessage)
{
    public static TreeSearchResults Empty { get; } = new([], null);

    public static TreeSearchResults Deferred(string message)
    {
        return new TreeSearchResults([], message);
    }

    public static TreeSearchResults Message(string message)
    {
        return new TreeSearchResults([], message);
    }
}