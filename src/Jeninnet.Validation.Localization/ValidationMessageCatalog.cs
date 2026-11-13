//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Localization;

/// <summary>
/// Copy-on-write message catalog. Reads are lock-free; writes are serialized and swap an immutable
/// snapshot so a lookup never observes a partially updated pack.
/// </summary>
internal sealed class ValidationMessageCatalog : IValidationMessageCatalog {
    private readonly Lock _writeLock = new();
    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _snapshot;

    /// <summary>
    /// Initializes a new catalog from an initial set of packs.
    /// </summary>
    /// <param name="initial">The initial packs.</param>
    /// <exception cref="ArgumentNullException"><paramref name="initial"/> is null.</exception>
    public ValidationMessageCatalog(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> initial) {
        ArgumentNullException.ThrowIfNull(initial);

        _snapshot = Snapshot(initial);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Cultures =>
        [.. Volatile.Read(ref _snapshot).Keys.OrderBy(static culture => culture, StringComparer.Ordinal)];

    /// <inheritdoc />
    public bool TryGetTemplate(string culture, string key, out string? template) {
        if(string.IsNullOrWhiteSpace(culture) || string.IsNullOrWhiteSpace(key)) {
            template = null;
            return false;
        }

        if(Volatile.Read(ref _snapshot).TryGetValue(culture, out var messages) &&
           messages.TryGetValue(key, out var found)) {
            template = found;
            return true;
        }

        template = null;
        return false;
    }

    /// <inheritdoc />
    public void SetTemplate(string culture, string key, string template) {
        var normalized = NormalizeCulture(culture);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(template);

        Mutate(copy => {
            if(!copy.TryGetValue(normalized, out var messages)) {
                messages = [with(StringComparer.Ordinal)];
                copy[normalized] = messages;
            }

            messages[key] = template;
        });
    }

    /// <inheritdoc />
    public bool RemoveTemplate(string culture, string key) {
        var normalized = NormalizeCulture(culture);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var removed = false;

        Mutate(copy => {
            if(!copy.TryGetValue(normalized, out var messages) || !messages.Remove(key)) {
                return;
            }

            removed = true;

            if(messages.Count is 0) {
                copy.Remove(normalized);
            }
        });

        return removed;
    }

    /// <inheritdoc />
    public void AddPack(string culture, IReadOnlyDictionary<string, string> messages) {
        ArgumentNullException.ThrowIfNull(messages);

        var normalized = NormalizeCulture(culture);

        Mutate(copy => {
            if(!copy.TryGetValue(normalized, out var target)) {
                target = [with(StringComparer.Ordinal)];
                copy[normalized] = target;
            }

            foreach(var message in messages) {
                target[message.Key] = message.Value;
            }
        });
    }

    /// <inheritdoc />
    public bool RemoveCulture(string culture) =>
        MutateRemove(NormalizeCulture(culture));

    private bool MutateRemove(string culture) {
        var removed = false;

        Mutate(copy => removed = copy.Remove(culture));

        return removed;
    }

    private void Mutate(Action<Dictionary<string, Dictionary<string, string>>> mutate) {
        lock(_writeLock) {
            var copy = Clone(Volatile.Read(ref _snapshot));
            mutate(copy);

            var frozen = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach(var pair in copy) {
                frozen[pair.Key] = pair.Value;
            }

            Volatile.Write(ref _snapshot, frozen);
        }
    }

    private static string NormalizeCulture(string culture) {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);

        return CultureInfo.GetCultureInfo(culture.Trim()).Name;
    }

    private static Dictionary<string, Dictionary<string, string>> Clone(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> source) {
        var copy = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach(var pair in source) {
            copy[pair.Key] = new Dictionary<string, string>(pair.Value, StringComparer.Ordinal);
        }

        return copy;
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> Snapshot(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> source) {
        var snapshot = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach(var pair in source) {
            snapshot[pair.Key] = new Dictionary<string, string>(pair.Value, StringComparer.Ordinal);
        }

        return snapshot;
    }
}
