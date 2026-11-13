//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization;

/// <summary>
/// A mutable, thread-safe catalog of validation message templates layered over the built-in
/// message packs. Changes take effect immediately for subsequent lookups.
/// </summary>
/// <remarks>
/// The catalog is process-local. Use the sibling localization store when messages must survive a
/// restart or be shared across instances. The catalog does not re-implement the exactly-one
/// <see cref="IInnerMessageProvider"/> rule; it is the implementation detail of the built-in
/// validation provider.
/// </remarks>
public interface IValidationMessageCatalog {
    /// <summary>
    /// Gets the cultures currently available in the catalog, in ordinal order.
    /// </summary>
    IReadOnlyCollection<string> Cultures { get; }

    /// <summary>
    /// Attempts to read one template from the catalog.
    /// </summary>
    /// <param name="culture">The exact culture code (for example <c>en</c>, <c>pt-BR</c>, or <c>zh-Hans</c>).</param>
    /// <param name="key">The validation message key.</param>
    /// <param name="template">The template when found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the culture and key exist.</returns>
    bool TryGetTemplate(string culture, string key, out string? template);

    /// <summary>
    /// Adds or replaces one template for a culture. A missing culture is created.
    /// </summary>
    /// <param name="culture">The culture code.</param>
    /// <param name="key">The validation message key.</param>
    /// <param name="template">The replacement template.</param>
    /// <exception cref="ArgumentException">A value is empty or whitespace, or the culture is invalid.</exception>
    void SetTemplate(string culture, string key, string template);

    /// <summary>
    /// Removes one template. The key falls back to the caller's <see cref="ValidationError.Message"/>
    /// when no other template applies.
    /// </summary>
    /// <param name="culture">The culture code.</param>
    /// <param name="key">The validation message key.</param>
    /// <returns><see langword="true"/> when a template was removed.</returns>
    /// <exception cref="ArgumentException">The culture or key is empty or whitespace.</exception>
    bool RemoveTemplate(string culture, string key);

    /// <summary>
    /// Adds or replaces a culture pack. Keys from the supplied pack win; other keys are preserved.
    /// </summary>
    /// <param name="culture">The culture code.</param>
    /// <param name="messages">The message templates keyed by validation message key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is null.</exception>
    /// <exception cref="ArgumentException">The culture is empty or invalid.</exception>
    void AddPack(string culture, IReadOnlyDictionary<string, string> messages);

    /// <summary>
    /// Removes a whole culture from the catalog.
    /// </summary>
    /// <param name="culture">The culture code.</param>
    /// <returns><see langword="true"/> when the culture existed.</returns>
    /// <exception cref="ArgumentException">The culture is empty or invalid.</exception>
    bool RemoveCulture(string culture);
}
