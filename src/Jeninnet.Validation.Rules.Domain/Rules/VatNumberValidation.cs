//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Validates the structural shape and check digits of supported VAT numbers.
/// </summary>
internal static class VatNumberValidation {
    private static readonly string[] _supportedCountries = ["BE", "DE", "FR", "GB", "NL"];

    /// <summary>
    /// Gets the supported VAT country codes in ordinal order.
    /// </summary>
    public static IReadOnlyCollection<string> SupportedCountries => _supportedCountries;

    /// <summary>
    /// Determines whether a VAT number is valid for the given country.
    /// </summary>
    /// <param name="value">The candidate VAT number.</param>
    /// <param name="country">The normalized country code.</param>
    /// <returns><see langword="true"/> when the number matches the country rules.</returns>
    public static bool IsValid(string value, string country) {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(country);

        if(value.Length > 32) {
            return false;
        }

        var compact = DomainCountry.Compact(value);

        return country switch {
            "NL" => IsDutchVat(compact),
            "DE" => IsGermanVat(compact),
            "FR" => IsFrenchVat(compact),
            "GB" => IsBritishVat(compact),
            "BE" => IsBelgianVat(compact),
            _ => false
        };
    }

    /// <summary>
    /// Validates the Dutch eleven-test (elfproef) for a BSN or btw-id.
    /// </summary>
    /// <param name="digits">Nine ASCII digits.</param>
    /// <returns><see langword="true"/> when the eleven-test passes.</returns>
    public static bool IsValidDutchElevenTest(ReadOnlySpan<char> digits) {
        if(digits.Length is not 9 || !DomainCountry.IsAsciiDigits(digits)) {
            return false;
        }

        var sum = 0;

        for(var i = 0; i < 8; i++) {
            sum += (digits[i] - '0') * (9 - i);
        }

        sum -= digits[8] - '0';

        return sum is not 0 && sum % 11 is 0;
    }

    private static bool IsDutchVat(string compact) {
        if(compact.Length is not 14 ||
           !compact.StartsWith("NL", StringComparison.Ordinal) ||
           compact[11] is not 'B') {
            return false;
        }

        return IsValidDutchElevenTest(compact.AsSpan(2, 9)) &&
               DomainCountry.IsAsciiDigits(compact.AsSpan(12, 2));
    }

    private static bool IsGermanVat(string compact) {
        if(compact.Length is not 11 ||
           !compact.StartsWith("DE", StringComparison.Ordinal) ||
           compact[2] is '0' ||
           !DomainCountry.IsAsciiDigits(compact.AsSpan(2, 9))) {
            return false;
        }

        var sum = 0;

        for(var i = 0; i < 8; i++) {
            sum += (compact[2 + i] - '0') * (9 - i);
        }

        var check = 10 - (sum % 11);

        if(check is 10) {
            check = 0;
        }

        return check == compact[10] - '0';
    }

    private static bool IsFrenchVat(string compact) {
        if(compact.Length is not 13 ||
           !compact.StartsWith("FR", StringComparison.Ordinal) ||
           !DomainCountry.IsAsciiDigits(compact.AsSpan(4, 9))) {
            return false;
        }

        if(!DomainCountry.IsAsciiDigits(compact.AsSpan(2, 2))) {
            return true;
        }

        var key = DomainCountry.ParseDigits(compact.AsSpan(2, 2));
        var siren = DomainCountry.ParseDigits(compact.AsSpan(4, 9));
        var expected = (12 + (3 * (siren % 97))) % 97;

        return key == expected;
    }

    private static bool IsBritishVat(string compact) {
        if(!compact.StartsWith("GB", StringComparison.Ordinal) ||
           compact.Length is not (11 or 14) ||
           !DomainCountry.IsAsciiDigits(compact.AsSpan(2, compact.Length - 2))) {
            return false;
        }

        var sum = 0;

        for(var i = 0; i < 7; i++) {
            sum += (compact[2 + i] - '0') * (8 - i);
        }

        var remainder = sum % 97;
        var check = remainder is 0 ? 0 : 97 - remainder;

        return check == ((compact[9] - '0') * 10) + (compact[10] - '0');
    }

    private static bool IsBelgianVat(string compact) {
        if(compact.Length is not 12 ||
           !compact.StartsWith("BE", StringComparison.Ordinal) ||
           compact[2] is not ('0' or '1') ||
           !DomainCountry.IsAsciiDigits(compact.AsSpan(2, 10))) {
            return false;
        }

        var baseNumber = DomainCountry.ParseDigits(compact.AsSpan(2, 8));
        var check = 97 - (baseNumber % 97);

        return check == DomainCountry.ParseDigits(compact.AsSpan(10, 2));
    }
}
