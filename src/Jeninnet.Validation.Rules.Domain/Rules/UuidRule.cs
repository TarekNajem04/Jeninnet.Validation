//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A rule for ensuring that a string member is a valid UUID.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message">The optional error message.</param>
public sealed class UuidRule<T>(
    IPropertyAccessor<T, string?> accessor,
    string? message = null)
    : UuidRule<T, string?>(accessor, message);

/// <summary>
/// A rule for ensuring that a string member is a valid UUID.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public class UuidRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    string? message,
    string? messageKey = ValidationMessageKeys.UUID)
    : SyncValidateRule<T, TProperty>(accessor, message, messageKey) {
    /// <inheritdoc />
    protected override void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(value is not string text || string.IsNullOrWhiteSpace(text)) {
            return;
        }

        if(!Guid.TryParse(text, out _)) {
            AddError(errors, context);
        }
    }
}
