//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a string member is a valid IP address.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message"   >The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class IPAddressRule<T>(IPropertyAccessor<T, string?> accessor, string? message, string? messageKey = ValidationMessageKeys.IP_ADDRESS)
    : IPAddressRule<T, string?>(accessor, message, messageKey);

/// <summary>
/// Ensures that a string member is a valid IP address.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type (string or nullable string).</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public class IPAddressRule<T, TProperty>(IPropertyAccessor<T, TProperty> accessor, string? message, string? messageKey = ValidationMessageKeys.IP_ADDRESS)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        if(value is not string s || string.IsNullOrEmpty(s)) {
            return;
        }

        if(!System.Net.IPAddress.TryParse(s, out _)) {
            AddError(errors, context);
        }
    }
}
