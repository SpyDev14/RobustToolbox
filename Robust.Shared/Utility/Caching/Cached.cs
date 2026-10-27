using System;

namespace Robust.Shared.Utility.Caching;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// No thread-safe
/// </remarks>
public sealed class Cached<T>
{
    private T _value = default!;
    private bool _cacheIsValid = false;
    private readonly Func<T> _factory;

    /// <summary>
    /// Value was invalidated (<see cref="Invalidate"/> was called
    /// when <see cref="Value"/> was actual).
    /// </summary>
    /// <remarks>
    /// Useful for invalidate dependent cache.
    /// </remarks>
    public event EventHandler? Invalidated;

    public Cached(Func<T> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Get value lazy. Was refreshed by given factory,
    /// sif cache was invalidated.
    /// </summary>
    public T Value
    {
        get
        {
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
    /// Does nothing if already invalidated.
    /// </para>
    /// <para>
    /// Drop reference to stoled value.
    /// </para>
    /// </remarks>
    public void Invalidate()
    {
        // Already invalidated, avoiding "event storm"
        if (!_cacheIsValid) return;

        _cacheIsValid = false;
        _value = default!; // drop reference
        Invalidated?.Invoke(this, EventArgs.Empty);
    }
}
