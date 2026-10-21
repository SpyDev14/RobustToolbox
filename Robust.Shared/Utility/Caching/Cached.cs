using System;

namespace Robust.Shared.Utility.Caching;


public struct Cached<T>
{
    private T _value = default!;
    private bool _cacheIsActual = false;
    private Func<T> _factory;

    public event EventHandler? CacheInvalidated;

    public Cached(Func<T> factory)
    {
        _factory = factory;
    }

    public T Value
    {
        get
        {
            if (!_cacheIsActual)
            {
                _value = _factory();
                _cacheIsActual = true;
            }

            return _value;
        }
    }

    public void Invalidate()
    {
        _cacheIsActual = false;
        _value = default!;
        CacheInvalidated?.Invoke(this, EventArgs.Empty);
    }
}
