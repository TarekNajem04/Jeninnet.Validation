//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Validates supported national identification numbers.
/// </summary>
internal static class NationalIdValidation {
    private static readonly string[] _supportedCountries = ["EG", "NL", "US"];

    /// <summary>
    /// Gets the supported national-ID country codes in ordinal order.
    /// </summary>
    public static IReadOnlyCollection<string> SupportedCountries => _supportedCountries;

    /// <summary>
    /// Determines whether a national identifier is valid for the given country.
    /// </summary>
    /// <param name="value">The candidate identifier.</param>
    /// <param name="country">The normalized country code.</param>
    /// <returns><see langword="true"/> when the identifier matches the country rules.</returns>
    public static bool IsValid(string value, string country) {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(country);

        if(value.Length > 32) {
            return false;
        }

        var compact = DomainCountry.Compact(value);

        return country switch {
            "NL" => VatNumberValidation.IsValidDutchElevenTest(compact),
            "EG" => IsEgyptianNationalId(compact),
            "US" => IsUnitedStatesSsn(compact),
            _ => false
        };
    }

    private static bool IsEgyptianNationalId(string compact) {
        if(compact.Length is not 14 ||
           !DomainCountry.IsAsciiDigits(compact) ||
           compact[0] is not ('2' or '3')) {
            return false;
        }

        var year = DomainCountry.ParseDigits(compact.AsSpan(1, 2));
        var month = DomainCountry.ParseDigits(compact.AsSpan(3, 2));
        var day = DomainCountry.ParseDigits(compact.AsSpan(5, 2));
        var fullYear = compact[0] is '2' ? 1900 + year : 2000 + year;
        var governorate = DomainCountry.ParseDigits(compact.AsSpan(7, 2));

        if(month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(fullYear, month)) {
            return false;
        }

        return governorate is (>= 1 and <= 35) or 88;
    }

    private static bool IsUnitedStatesSsn(string compact) {
        if(compact.Length is not 9 || !DomainCountry.IsAsciiDigits(compact)) {
            return false;
        }

        var area = DomainCountry.ParseDigits(compact.AsSpan(0, 3));
        var group = DomainCountry.ParseDigits(compact.AsSpan(3, 2));
        var serial = DomainCountry.ParseDigits(compact.AsSpan(5, 4));

        return area is not 0 and not 666 and (< 900) &&
               group is not 0 &&
               serial is not 0;
    }
}
