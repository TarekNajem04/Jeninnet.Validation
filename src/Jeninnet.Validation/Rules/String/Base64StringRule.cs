//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A rule for ensuring that a string member is a valid Base64 string. Whitespace is ignored, null
/// is valid, and validation is allocation-free.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class Base64StringRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    string? message,
    string? messageKey = ValidationMessageKeys.BASE64_STRING)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not string text) {
            return;
        }

        if(!Base64Validation.IsValid(text)) {
            AddError(errors, context);
        }
    }
}

file static class Base64Validation {
    /// <summary>
    /// Validates a Base64 payload without allocating. Whitespace is ignored; the payload must have
    /// a multiple-of-four length, use the standard alphabet, and place at most two padding
    /// characters at the end.
    /// </summary>
    /// <param name="value">The candidate payload.</param>
    /// <returns><see langword="true"/> when the payload is valid Base64.</returns>
    public static bool IsValid(string value) {
        ArgumentNullException.ThrowIfNull(value);

        var payloadLength = 0;
        var padding = 0;

        foreach(var character in value) {
            if(char.IsWhiteSpace(character)) {
                continue;
            }

            payloadLength++;

            if(character == '=') {
                padding++;

                if(padding > 2) {
                    return false;
                }

                continue;
            }

            if(padding > 0 || !IsBase64Character(character)) {
                return false;
            }
        }

        return payloadLength == 0 || payloadLength % 4 == 0;
    }

    private static bool IsBase64Character(char character) =>
        character is (>= 'A' and <= 'Z')
        or (>= 'a' and <= 'z')
        or (>= '0' and <= '9')
        or '+'
        or '/';
}
