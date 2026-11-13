//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization;

/// <summary>
/// Configures additional validation message packs and per-key overrides for the built-in
/// <see cref="DefaultValidationMessageProvider"/>.
/// </summary>
/// <remarks>
/// The sibling localization platform enforces exactly one registered
/// <see cref="IInnerMessageProvider"/>. This options object is therefore the supported way to
/// extend or override the built-in packs: custom messages are merged over the built-in templates
/// inside that single provider.
/// </remarks>
public sealed class ValidationMessagePackOptions {
    private readonly Dictionary<string, Dictionary<string, string>> _packs = [with(StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Adds or replaces a culture pack. Existing keys from the same culture are merged, with the
    /// supplied pack winning.
    /// </summary>
    /// <param name="pack">The message pack.</param>
    /// <returns>The same options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pack"/> is null.</exception>
    public ValidationMessagePackOptions AddPack(ValidationMessagePack pack) {
        ArgumentNullException.ThrowIfNull(pack);

        var target = GetOrCreate(pack.Culture);

        foreach(var message in pack.Messages) {
            target[message.Key] = message.Value;
        }

        return this;
    }

    /// <summary>
    /// Adds or replaces a culture pack from a message dictionary.
    /// </summary>
    /// <param name="culture">The culture code (for example <c>"fr"</c>).</param>
    /// <param name="messages">The message templates keyed by validation message key.</param>
    /// <returns>The same options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="culture"/> is empty or whitespace.</exception>
    public ValidationMessagePackOptions AddPack(string culture, IReadOnlyDictionary<string, string> messages) =>
        AddPack(new ValidationMessagePack(culture, messages));

    /// <summary>
    /// Overrides one message template for a culture.
    /// </summary>
    /// <param name="culture">The culture code (for example <c>"en"</c>).</param>
    /// <param name="key">The validation message key.</param>
    /// <param name="template">The replacement template.</param>
    /// <returns>The same options instance for chaining.</returns>
    /// <exception cref="ArgumentException">A value is empty or whitespace.</exception>
    public ValidationMessagePackOptions Override(string culture, string key, string template) {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(template);

        GetOrCreate(CultureInfo.GetCultureInfo(culture).Name)[key] = template;
        return this;
    }

    internal IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Build() {
        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach(var pack in _packs) {
            result[pack.Key] = pack.Value;
        }

        return result;
    }

    private Dictionary<string, string> GetOrCreate(string culture) {
        if(!_packs.TryGetValue(culture, out var messages)) {
            messages = [with(StringComparer.Ordinal)];
            _packs[culture] = messages;
        }

        return messages;
    }
}
