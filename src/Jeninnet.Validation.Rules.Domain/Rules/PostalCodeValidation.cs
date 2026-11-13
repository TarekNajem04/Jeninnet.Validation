//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Validates supported national postal-code formats.
/// </summary>
internal static partial class PostalCodeValidation {
    private static readonly string[] _supportedCountries = ["BE", "DE", "EG", "FR", "GB", "NL", "US"];

    /// <summary>
    /// Gets the supported postal-code country codes in ordinal order.
    /// </summary>
    public static IReadOnlyCollection<string> SupportedCountries => _supportedCountries;

    /// <summary>
    /// Determines whether a postal code is valid for the given country.
    /// </summary>
    /// <param name="value">The candidate postal code.</param>
    /// <param name="country">The normalized country code.</param>
    /// <returns><see langword="true"/> when the postal code matches the country format.</returns>
    public static bool IsValid(string value, string country) {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(country);

        if(value.Length > 32) {
            return false;
        }

        var compact = DomainCountry.Compact(value);

        return country switch {
            "NL" => DutchPostalCode().IsMatch(compact),
            "US" => UnitedStatesPostalCode().IsMatch(value.Trim()),
            "DE" or "FR" => compact.Length is 5 &&
                             compact[0] is not '0' &&
                             DomainCountry.IsAsciiDigits(compact),
            "BE" => compact.Length is 4 &&
                    compact[0] is not '0' &&
                    DomainCountry.IsAsciiDigits(compact),
            "EG" => compact.Length is 5 && DomainCountry.IsAsciiDigits(compact),
            "GB" => BritishPostalCode().IsMatch(compact),
            _ => false
        };
    }

    [GeneratedRegex(@"^[1-9][0-9]{3}(?!SA|SD|SS)[A-Z]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex DutchPostalCode();

    [GeneratedRegex(@"^[0-9]{5}(-[0-9]{4})?$", RegexOptions.CultureInvariant)]
    private static partial Regex UnitedStatesPostalCode();

    [GeneratedRegex(@"^(GIR0AA|[A-PR-UWYZ][A-HK-Y]?[0-9][0-9A-Z]?[0-9][ABD-HJLNP-UW-Z]{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex BritishPostalCode();
}
