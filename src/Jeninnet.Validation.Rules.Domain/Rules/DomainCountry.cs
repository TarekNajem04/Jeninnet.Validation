//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Normalizes and validates explicit country codes used by domain rules.
/// </summary>
internal static class DomainCountry {
    /// <summary>
    /// Normalizes a required two-letter country code.
    /// </summary>
    /// <param name="country">The country code.</param>
    /// <returns>The upper-case, trimmed country code.</returns>
    /// <exception cref="ArgumentException"><paramref name="country"/> is empty or not two ASCII letters.</exception>
    public static string Normalize(string country) {
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        var normalized = country.Trim().ToUpperInvariant();

        if(normalized.Length is not 2 ||
           !char.IsAsciiLetter(normalized[0]) ||
           !char.IsAsciiLetter(normalized[1])) {
            throw new ArgumentException(
                "A country code must contain exactly two ASCII letters.",
                nameof(country));
        }

        return normalized;
    }

    /// <summary>
    /// Normalizes an optional two-letter country code.
    /// </summary>
    /// <param name="country">The country code, or <see langword="null"/>.</param>
    /// <returns>The normalized country code, or <see langword="null"/>.</returns>
    public static string? NormalizeOptional(string? country) =>
        country is null ? null : Normalize(country);

    /// <summary>
    /// Throws when a rule does not support the requested country.
    /// </summary>
    /// <param name="country">The normalized country code.</param>
    /// <param name="supported">The supported country codes.</param>
    /// <param name="ruleName">The rule name used in the exception message.</param>
    /// <exception cref="NotSupportedException">The country is not supported by the rule.</exception>
    public static void EnsureSupported(
        string country,
        IReadOnlyCollection<string> supported,
        string ruleName) {
        ArgumentNullException.ThrowIfNull(supported);

        if(supported.Contains(country)) {
            return;
        }

        throw new NotSupportedException(
            $"The {ruleName} rule does not support country '{country}'. " +
            $"Supported countries: {string.Join(", ", supported)}.");
    }

    /// <summary>
    /// Removes common separators from a formatted identifier.
    /// </summary>
    /// <param name="value">The formatted value.</param>
    /// <returns>The compact, upper-case value.</returns>
    public static string Compact(string value) {
        ArgumentNullException.ThrowIfNull(value);

        var buffer = value.Length <= 64
            ? stackalloc char[value.Length]
            : new char[value.Length];

        var length = 0;

        foreach(var character in value) {
            if(char.IsWhiteSpace(character) ||
               character is '-' or '.' or '/' or '_') {
                continue;
            }

            buffer[length++] = char.ToUpperInvariant(character);
        }

        return new string(buffer[..length]);
    }

    /// <summary>
    /// Determines whether a span contains only ASCII digits.
    /// </summary>
    /// <param name="value">The candidate value.</param>
    /// <returns><see langword="true"/> when every character is a digit.</returns>
    public static bool IsAsciiDigits(ReadOnlySpan<char> value) {
        foreach(var character in value) {
            if(!char.IsAsciiDigit(character)) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Parses an ASCII digit span into an integer.
    /// </summary>
    /// <param name="value">The digit span.</param>
    /// <returns>The parsed value.</returns>
    public static int ParseDigits(ReadOnlySpan<char> value) {
        var result = 0;

        foreach(var character in value) {
            result = (result * 10) + (character - '0');
        }

        return result;
    }
}
