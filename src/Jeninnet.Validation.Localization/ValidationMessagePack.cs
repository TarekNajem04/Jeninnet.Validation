//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Localization;

/// <summary>
/// A named set of validation message templates for one culture.
/// </summary>
public sealed class ValidationMessagePack {
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationMessagePack"/> class.
    /// </summary>
    /// <param name="culture">The culture code (for example <c>"en"</c>, <c>"pt-BR"</c>, or <c>"zh-Hans"</c>).</param>
    /// <param name="messages">The message templates keyed by <see cref="ValidationMessageKeys"/> values.</param>
    /// <exception cref="ArgumentException"><paramref name="culture"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is null.</exception>
    public ValidationMessagePack(string culture, IReadOnlyDictionary<string, string> messages) {
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);
        ArgumentNullException.ThrowIfNull(messages);

        Culture = CultureInfo.GetCultureInfo(culture).Name;
        Messages = messages;
    }

    /// <summary>
    /// Gets the normalized culture name, preserving script and region subtags such as
    /// <c>zh-Hans</c> and <c>pt-BR</c>.
    /// </summary>
    public string Culture { get; }

    /// <summary>
    /// Gets the message templates keyed by validation message key.
    /// </summary>
    public IReadOnlyDictionary<string, string> Messages { get; }
}
