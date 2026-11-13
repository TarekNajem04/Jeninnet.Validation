//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a string member is a valid phone number.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message"   >The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class PhoneRule<T>(IPropertyAccessor<T, string?> accessor, string? message, string? messageKey = ValidationMessageKeys.PHONE)
    : PhoneRule<T, string?>(accessor, message, messageKey);

/// <summary>
/// Ensures that a string member is a valid phone number.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public class PhoneRule<T, TProperty>(IPropertyAccessor<T, TProperty> accessor, string? message, string? messageKey = ValidationMessageKeys.PHONE)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        if(value is not string s || string.IsNullOrWhiteSpace(s)) {
            return; // Use NotEmpty for mandatory checks
        }

        if(!StringRegexes.Phone.IsMatch(s)) {
            AddError(errors, context);
        }
    }
}
