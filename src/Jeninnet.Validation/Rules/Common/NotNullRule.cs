//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A specialized, type-specific rule for ensuring that a member is not null.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor for the member.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class NotNullRule<T, TValue>(IPropertyAccessor<T, TValue> accessor, string? message, string? messageKey = ValidationMessageKeys.NOT_NULL)
    : SyncValidateRule<T, TValue>(accessor, message, messageKey) {
    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(value is null) {
            AddError(errors, context);
        }
    }
}
