//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A specialized, type-specific rule for ensuring that a collection member is not empty.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="message">The custom validation message, if any.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class NotEmptyRule<T, TValue>(IPropertyAccessor<T, TValue> accessor, string? message, string? messageKey = ValidationMessageKeys.NOT_EMPTY)
    : SyncValidateRule<T, TValue>(accessor, message, messageKey) {
    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(ValueEmptiness.IsEmpty(value)) {
            AddError(errors, context);
        }
    }
}
