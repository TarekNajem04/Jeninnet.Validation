//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Validates that the value is <see langword="null"/>.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message">The validation message.</param>
/// <param name="messageKey">The validation message key.</param>
public sealed class NullRule<T, TValue>(IPropertyAccessor<T, TValue> accessor, string? message, string? messageKey = ValidationMessageKeys.NULL)
    : SyncValidateRule<T, TValue>(accessor, message, messageKey) {
    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(value is not null) {
            AddError(errors, context);
        }
    }
}
