//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A rule for ensuring that a string member is a valid postal code for a supported country.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="country">The required postal-code country code.</param>
/// <param name="message">The optional error message.</param>
public sealed class PostalCodeRule<T>(
    IPropertyAccessor<T, string?> accessor,
    string country,
    string? message = null)
    : PostalCodeRule<T, string?>(accessor, country, message);

/// <summary>
/// A rule for ensuring that a string member is a valid postal code for a supported country.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="country">The required postal-code country code.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
/// <exception cref="ArgumentException"><paramref name="country"/> is not a two-letter code.</exception>
/// <exception cref="NotSupportedException"><paramref name="country"/> is not supported.</exception>
public class PostalCodeRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    string country,
    string? message,
    string? messageKey = ValidationMessageKeys.POSTAL_CODE)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the required postal-code country code.
    /// </summary>
    public string Country { get; } = NormalizeCountry(country);

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not string text || string.IsNullOrWhiteSpace(text)) {
            return;
        }

        if(!PostalCodeValidation.IsValid(text, Country)) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) =>
        new Dictionary<string, object> { { MessageParameterNames.Country, Country } };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> { { MessageParameterNames.Country, Country } };

    private static string NormalizeCountry(string country) {
        var normalized = DomainCountry.Normalize(country);
        DomainCountry.EnsureSupported(normalized, PostalCodeValidation.SupportedCountries, "PostalCode");
        return normalized;
    }
}
