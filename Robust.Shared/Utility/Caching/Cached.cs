using System;
using System.Diagnostics;

namespace Robust.Shared.Utility.Caching;

/// <remarks>
/// No thread-safe
/// </remarks>
public struct Cached<T>
{
    private T _value = default!;
    private bool _cacheIsValid = false;
    private readonly Func<T> _factory;

    /// <summary>
    /// Value was invalidated (<see cref="Invalidate"/> was called
    /// when <see cref="Value"/> was actual).
    /// </summary>
    /// <remarks>
    /// <para>
    ///   Useful for invalidate dependent cache.
    /// </para>
    /// </remarks>
    public event Action? Invalidated;

    public Cached(Func<T> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Get value lazy. Was refreshed by given factory,
    /// if cache was invalidated.
    /// </summary>
    public T Value
    {
        get
        {
            // null for default(T)
            Debug.Assert(_factory is not null);

            if (!_cacheIsValid)
            {
                _value = _factory();
                _cacheIsValid = true;
            }

            return _value;
        }
    }

    /// <summary>
    /// Mark cache as invalidated and raise <see cref="Invalidated"/>
    /// event.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   Does nothing if already invalidated.
    /// </para>
    /// <para>
    ///   Drop reference to stoled value.
    /// </para>
    /// </remarks>
    public void Invalidate()
    {
        // Already invalidated, avoiding event storm
        if (!_cacheIsValid) return;

        _cacheIsValid = false;
        _value = default!; // drop reference
        Invalidated?.Invoke();
    }
}
