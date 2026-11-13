//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Sanitization;

/// <summary>
/// Pure string sanitizers. Every function preserves <see langword="null"/> and is
/// deterministic: casing uses the invariant culture unless a culture-specific overload is
/// chosen explicitly.
/// </summary>
public static class Sanitize {
    /// <summary>
    /// Removes leading and trailing white space. Preserves <see langword="null"/>.
    /// </summary>
    /// <param name="value">The value to trim.</param>
    /// <returns>The trimmed value, or <see langword="null"/> when <paramref name="value"/> is null.</returns>
    public static string? Trim(string? value) => value?.Trim();

    /// <summary>
    /// Converts the value to upper case using the invariant culture. Preserves <see langword="null"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The upper-cased value, or <see langword="null"/> when <paramref name="value"/> is null.</returns>
    public static string? ToUpperInvariant(string? value) => value?.ToUpperInvariant();

    /// <summary>
    /// Converts the value to lower case using the invariant culture. Preserves <see langword="null"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The lower-cased value, or <see langword="null"/> when <paramref name="value"/> is null.</returns>
    public static string? ToLowerInvariant(string? value) => value?.ToLowerInvariant();

    /// <summary>
    /// Converts the value to upper case using the specified culture. Preserves <see langword="null"/>.
    /// </summary>
    /// <param name="culture">The culture used for casing.</param>
    /// <returns>A sanitizer that upper-cases with <paramref name="culture"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="culture"/> is null. Use <see cref="ToUpperInvariant"/> for the invariant default.</exception>
    public static Func<string?, string?> ToUpper(CultureInfo culture) {
        ArgumentNullException.ThrowIfNull(culture);

        return value => value?.ToUpper(culture);
    }

    /// <summary>
    /// Converts the value to lower case using the specified culture. Preserves <see langword="null"/>.
    /// </summary>
    /// <param name="culture">The culture used for casing.</param>
    /// <returns>A sanitizer that lower-cases with <paramref name="culture"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="culture"/> is null. Use <see cref="ToLowerInvariant"/> for the invariant default.</exception>
    public static Func<string?, string?> ToLower(CultureInfo culture) {
        ArgumentNullException.ThrowIfNull(culture);

        return value => value?.ToLower(culture);
    }

    /// <summary>
    /// Collapses every run of white-space characters to a single space and trims the ends.
    /// Preserves <see langword="null"/>.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>The normalized value, or <see langword="null"/> when <paramref name="value"/> is null.</returns>
    public static string? NormalizeWhitespace(string? value) {
        if(string.IsNullOrEmpty(value)) {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach(var character in value) {
            if(char.IsWhiteSpace(character)) {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if(pendingSpace) {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Removes every Unicode control character except horizontal tab, line feed, and carriage
    /// return. Preserves <see langword="null"/>. This is plain control-character stripping, not
    /// HTML or markup sanitization; use a dedicated library for untrusted markup.
    /// </summary>
    /// <param name="value">The value to clean.</param>
    /// <returns>The value without control characters, or <see langword="null"/> when <paramref name="value"/> is null.</returns>
    public static string? RemoveControlChars(string? value) {
        if(string.IsNullOrEmpty(value)) {
            return value;
        }

        var builder = new StringBuilder(value.Length);

        foreach(var character in value.Where(static character =>
                     character is '\t' or '\n' or '\r' || !char.IsControl(character))) {
            builder.Append(character);
        }

        return builder.Length == value.Length ? value : builder.ToString();
    }

    /// <summary>
    /// Applies an element sanitizer to every element of a string list in place and returns the
    /// same list instance. Preserves <see langword="null"/> lists and <see langword="null"/> elements.
    /// </summary>
    /// <param name="elementSanitizer">The per-element sanitizer.</param>
    /// <returns>A sanitizer for string lists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="elementSanitizer"/> is null.</exception>
    public static Func<IList<string?>?, IList<string?>?> EachElement(Func<string?, string?> elementSanitizer) {
        ArgumentNullException.ThrowIfNull(elementSanitizer);

        return list => {
            if(list is null) {
                return null;
            }

            for(var index = 0; index < list.Count; index++) {
                list[index] = elementSanitizer(list[index]);
            }

            return list;
        };
    }
}
