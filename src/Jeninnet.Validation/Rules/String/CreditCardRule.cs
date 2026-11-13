//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A specialized, type-specific rule for ensuring that a string member is a valid credit card number.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message"   >The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class CreditCardRule<T>(IPropertyAccessor<T, string?> accessor, string? message, string? messageKey = ValidationMessageKeys.CREDIT_CARD)
    : CreditCardRule<T, string?>(accessor, message, messageKey);

/// <summary>
/// Ensures that a string member is a valid credit card number.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type (string or nullable string).</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public class CreditCardRule<T, TProperty>(IPropertyAccessor<T, TProperty> accessor, string? message, string? messageKey = ValidationMessageKeys.CREDIT_CARD)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not string s || string.IsNullOrEmpty(s)) {
            return;
        }

        if(!StringRegexes.CreditCard.IsMatch(s) || !IsValidLuhn(s)) {
            AddError(errors, context);
        }
    }

    /// <summary>
    /// The Luhn algorithm, also known as the modulus 10 or mod 10 algorithm,
    /// is a simple checksum formula used to validate a variety of identification numbers, such as credit card numbers,
    /// IMEI numbers, Canadian Social Insurance Numbers.
    /// The LUHN formula was created in the late 1960s by a group of mathematicians.
    /// Shortly thereafter, credit card companies adopted it.
    /// Because the algorithm is in the public domain, it can be used by anyone.
    /// Most credit cards and many government identification numbers use the algorithm as a simple method of distinguishing valid numbers from mistyped or otherwise incorrect numbers.
    /// It was designed to protect against accidental errors, not malicious attacks.
    /// </summary>
    /// <param name="number">The credit card number to validate.</param>
    /// <returns>true if the number is valid according to the Luhn algorithm; otherwise, false.</returns>
    private static bool IsValidLuhn(string number) {
        var sum = 0;
        var alternate = false;

        for(var i = number.Length - 1; i >= 0; i--) {
            var n = int.Parse(number[i].ToString(), CultureInfo.InvariantCulture);
            if(alternate) {
                n *= 2;
                if(n > 9) {
                    n -= 9;
                }
            }

            sum += n;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }
}
