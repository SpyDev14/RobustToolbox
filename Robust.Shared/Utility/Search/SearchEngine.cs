using System;
using System.Collections.Generic;

namespace Robust.Shared.Utility.Search;



public interface ISearchEngine<T>
{
    bool IsMatch(string textToSearch, T element);
    bool IsMatch(string textToSearch, T element, StringComparison comparisonMode);
}

internal sealed class DefaultSearchEngine<T> : ISearchEngine<T>
{
    private Func<T, IReadOnlyList<string?>> _extractTextFromT;

    /// <param name="extractTextFromT">
    ///   Should returns strings to search by priority.
    ///   Like name and description.
    /// </param>
    public DefaultSearchEngine(Func<T, IReadOnlyList<string?>> extractTextFromT)
    {
        _extractTextFromT = extractTextFromT;
    }

    public bool IsMatch(string searchText, T element)
        => IsMatch(searchText, element, StringComparison.CurrentCultureIgnoreCase);

    public bool IsMatch(string searchText, T element, StringComparison comparsionMode)
    {
        foreach (var text in _extractTextFromT(element))
        {
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (text.Contains(searchText.Trim(), comparsionMode))
                return true;
        }

        return false;
    }
}
