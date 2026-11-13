//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// A per-walk bag for sharing memoized values between validation rules. The backing dictionary is
/// allocated lazily on the first write; read-only probes do not allocate. Instances are pooled per
/// thread by the engine. Not thread-safe: validation walks are sequential by design.
/// </summary>
public sealed class RootContextData {
    [ThreadStatic]
    private static RootContextData? _cachedRootData;

    private Dictionary<string, object?>? _values;
    private Dictionary<object, object?>? _state;

    internal ValidationFilter Filter { get; set; }

    /// <summary>
    /// Gets the number of values currently stored in the bag.
    /// </summary>
    public int Count => _values?.Count ?? 0;

    /// <summary>
    /// Gets or sets a value by key. The backing dictionary is allocated on the first write.
    /// </summary>
    /// <param name="key">The value key.</param>
    /// <returns>The stored value, or <see langword="null"/> when the key is absent.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty or whitespace.</exception>
    public object? this[string key] {
        get {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            return _values is not null && _values.TryGetValue(key, out var value) ? value : null;
        }
        set {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            _values ??= [with(StringComparer.Ordinal)];
            _values[key] = value;
        }
    }

    /// <summary>
    /// Tries to read a value by key without allocating.
    /// </summary>
    /// <param name="key">The value key.</param>
    /// <param name="value">The stored value when found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the key exists.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty or whitespace.</exception>
    public bool TryGetValue(string key, out object? value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if(_values is not null && _values.TryGetValue(key, out value)) {
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Returns the value for <paramref name="key"/>, creating it with
    /// <paramref name="factory"/> when absent. This is the memoization helper for shared work
    /// (for example, a single database lookup reused by multiple rules).
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="key">The value key.</param>
    /// <param name="factory">The factory invoked when the key is absent.</param>
    /// <returns>The existing or newly created value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty or whitespace.</exception>
    public TValue GetOrAdd<TValue>(string key, Func<TValue> factory) {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        if(_values is not null && _values.TryGetValue(key, out var existing) && existing is TValue typed) {
            return typed;
        }

        var created = factory();
        this[key] = created;
        return created;
    }

    /// <summary>
    /// Removes all values. The dictionaries remain allocated for reuse.
    /// </summary>
    public void Clear() {
        _values?.Clear();
        _state?.Clear();
        Filter = default;
    }

    internal void SetState(object key, object value) {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        _state ??= [with(ReferenceEqualityComparer.Instance)];
        _state[key] = value;
    }

    internal bool TryGetState(object key, out object? value) {
        ArgumentNullException.ThrowIfNull(key);

        if(_state is not null && _state.TryGetValue(key, out value)) {
            return true;
        }

        value = null;
        return false;
    }

    internal static RootContextData Rent() {
        var cached = _cachedRootData;

        if(cached is not null) {
            _cachedRootData = null;
            return cached;
        }

        return new RootContextData();
    }

    internal static void Return(RootContextData data) {
        data.Clear();
        _cachedRootData = data;
    }
}
