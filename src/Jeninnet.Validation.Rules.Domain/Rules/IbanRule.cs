//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A rule for ensuring that a string member is a valid IBAN.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="country">The optional required IBAN country code.</param>
/// <param name="message">The optional error message.</param>
public sealed class IbanRule<T>(
    IPropertyAccessor<T, string?> accessor,
    string? country = null,
    string? message = null)
    : IbanRule<T, string?>(accessor, country, message);

/// <summary>
/// A rule for ensuring that a string member is a valid IBAN.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="country">The optional required IBAN country code.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
/// <exception cref="ArgumentException"><paramref name="country"/> is not a two-letter code.</exception>
/// <exception cref="NotSupportedException"><paramref name="country"/> is not an IBAN country.</exception>
public class IbanRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    string? country,
    string? message,
    string? messageKey = ValidationMessageKeys.IBAN)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <summary>
    /// Gets the optional required IBAN country code.
    /// </summary>
    public string? Country { get; } = NormalizeCountry(country);

    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not string text || string.IsNullOrWhiteSpace(text)) {
            return;
        }

        if(!IbanValidation.IsValid(text, Country)) {
            AddError(errors, context);
        }
    }

    /// <inheritdoc />
    protected override IDictionary<string, object>? GetMessageParameters(ValidationContext<T> context) =>
        new Dictionary<string, object> {
            { MessageParameterNames.Country, Country ?? string.Empty }
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, object?> GetMetadataParameters() =>
        new Dictionary<string, object?> { { MessageParameterNames.Country, Country } };

    private static string? NormalizeCountry(string? country) {
        if(country is null) {
            return null;
        }

        var normalized = DomainCountry.Normalize(country);
        DomainCountry.EnsureSupported(normalized, IbanValidation.SupportedCountries, "Iban");
        return normalized;
    }
}
