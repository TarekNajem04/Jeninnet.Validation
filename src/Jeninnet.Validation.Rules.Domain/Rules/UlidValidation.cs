//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Validates ULID strings in Crockford Base32 form.
/// </summary>
internal static class UlidValidation {
    /// <summary>
    /// Determines whether a value is a canonical ULID.
    /// </summary>
    /// <param name="value">The candidate value; case is ignored.</param>
    /// <returns><see langword="true"/> when the value has 26 Crockford characters and fits 128 bits.</returns>
    public static bool IsValid(string value) {
        ArgumentNullException.ThrowIfNull(value);

        if(value.Length is not 26) {
            return false;
        }

        var first = char.ToUpperInvariant(value[0]);

        if(first is < '0' or > '7') {
            return false;
        }

        foreach(var character in value) {
            var upper = char.ToUpperInvariant(character);

            if(upper is (>= '0' and <= '9') or
               (>= 'A' and <= 'H') or
               (>= 'J' and <= 'K') or
               (>= 'M' and <= 'N') or
               (>= 'P' and <= 'T') or
               (>= 'V' and <= 'Z')) {
                continue;
            }

            return false;
        }

        return true;
    }
}
