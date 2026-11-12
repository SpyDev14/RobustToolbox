using System;
using System.Collections.Generic;

namespace Robust.Shared.Utility.Search;

public interface ISearchEngineFactory
{
    ISearchEngine<T> GetEngine<T>(Func<T, IReadOnlyList<string?>> extractTextFromT);
}

internal sealed class DefaultSearchEngineFactory : ISearchEngineFactory
{
    public ISearchEngine<T> GetEngine<T>(Func<T, IReadOnlyList<string?>> extractTextFromT)
    {
        return new DefaultSearchEngine<T>(extractTextFromT);
    }
}
