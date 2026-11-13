//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Validates the structural shape and mod-97 checksum of an IBAN.
/// </summary>
internal static class IbanValidation {
    private static readonly Dictionary<string, int> _lengths = new(StringComparer.Ordinal) {
        ["AD"] = 24,
        ["AE"] = 23,
        ["AL"] = 28,
        ["AT"] = 20,
        ["AZ"] = 28,
        ["BA"] = 20,
        ["BE"] = 16,
        ["BG"] = 22,
        ["BH"] = 22,
        ["BI"] = 27,
        ["BR"] = 29,
        ["BY"] = 28,
        ["CH"] = 21,
        ["CR"] = 22,
        ["CY"] = 28,
        ["CZ"] = 24,
        ["DE"] = 22,
        ["DJ"] = 27,
        ["DK"] = 18,
        ["DO"] = 28,
        ["EE"] = 20,
        ["EG"] = 29,
        ["ES"] = 24,
        ["FI"] = 18,
        ["FK"] = 18,
        ["FO"] = 18,
        ["FR"] = 27,
        ["GB"] = 22,
        ["GE"] = 22,
        ["GI"] = 23,
        ["GL"] = 18,
        ["GR"] = 27,
        ["GT"] = 28,
        ["HR"] = 21,
        ["HU"] = 28,
        ["IE"] = 22,
        ["IL"] = 23,
        ["IQ"] = 23,
        ["IS"] = 26,
        ["IT"] = 27,
        ["JO"] = 30,
        ["KW"] = 30,
        ["KZ"] = 20,
        ["LB"] = 28,
        ["LC"] = 32,
        ["LI"] = 21,
        ["LT"] = 20,
        ["LU"] = 20,
        ["LV"] = 21,
        ["MC"] = 27,
        ["MD"] = 24,
        ["ME"] = 22,
        ["MK"] = 19,
        ["MR"] = 27,
        ["MT"] = 31,
        ["MU"] = 30,
        ["NL"] = 18,
        ["NO"] = 15,
        ["OM"] = 23,
        ["PK"] = 24,
        ["PL"] = 28,
        ["PS"] = 29,
        ["PT"] = 25,
        ["QA"] = 29,
        ["RO"] = 24,
        ["RS"] = 22,
        ["SA"] = 24,
        ["SC"] = 31,
        ["SD"] = 18,
        ["SE"] = 24,
        ["SI"] = 19,
        ["SK"] = 24,
        ["SM"] = 27,
        ["SO"] = 23,
        ["SS"] = 18,
        ["ST"] = 25,
        ["SV"] = 28,
        ["SY"] = 24,
        ["TL"] = 23,
        ["TN"] = 24,
        ["TR"] = 26,
        ["TZ"] = 31,
        ["UA"] = 29,
        ["VA"] = 22,
        ["VG"] = 24,
        ["XK"] = 20,
        ["YE"] = 30
    };

    /// <summary>
    /// Gets the supported IBAN country codes in ordinal order.
    /// </summary>
    public static IReadOnlyCollection<string> SupportedCountries =>
        [.. _lengths.Keys.OrderBy(static country => country, StringComparer.Ordinal)];

    /// <summary>
    /// Determines whether an IBAN is structurally valid.
    /// </summary>
    /// <param name="value">The candidate IBAN; spaces and hyphens are ignored.</param>
    /// <param name="expectedCountry">The optional required country code.</param>
    /// <returns><see langword="true"/> when the IBAN has a known length and a valid mod-97 checksum.</returns>
    public static bool IsValid(string value, string? expectedCountry) {
        ArgumentNullException.ThrowIfNull(value);

        if(value.Length > 64) {
            return false;
        }

        var compact = DomainCountry.Compact(value);

        if(compact.Length is < 15 or > 34) {
            return false;
        }

        if(!char.IsAsciiLetter(compact[0]) ||
           !char.IsAsciiLetter(compact[1]) ||
           !char.IsAsciiDigit(compact[2]) ||
           !char.IsAsciiDigit(compact[3])) {
            return false;
        }

        var country = compact[..2];

        if(!_lengths.TryGetValue(country, out var expectedLength) || compact.Length != expectedLength) {
            return false;
        }

        if(expectedCountry is not null &&
           !string.Equals(expectedCountry, country, StringComparison.Ordinal)) {
            return false;
        }

        var remainder = 0;

        for(var i = 4; i < compact.Length; i++) {
            remainder = Accumulate(compact[i], remainder);

            if(remainder < 0) {
                return false;
            }
        }

        for(var i = 0; i < 4; i++) {
            remainder = Accumulate(compact[i], remainder);

            if(remainder < 0) {
                return false;
            }
        }

        return remainder is 1;
    }

    private static int Accumulate(char character, int remainder) {
        if(character is >= '0' and <= '9') {
            return ((remainder * 10) + (character - '0')) % 97;
        }

        if(character is >= 'A' and <= 'Z') {
            return ((remainder * 100) + character - 'A' + 10) % 97;
        }

        return -1;
    }
}
