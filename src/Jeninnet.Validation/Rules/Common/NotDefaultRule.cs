//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a member is not equal to the default value of its type. Unlike
/// <see cref="NotEmptyRule{T,TValue}"/>, this rule treats <c>0</c>, <c>false</c>,
/// <c>Guid.Empty</c>, and a default <see cref="DateTime"/> as failures.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class NotDefaultRule<T, TValue>(
    IPropertyAccessor<T, TValue> accessor,
    string? message,
    string? messageKey = ValidationMessageKeys.NOT_DEFAULT)
    : SyncValidateRule<T, TValue>(accessor, message, messageKey) {
    /// <inheritdoc />
    protected override void ValidateValue(TValue value, in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(EqualityComparer<TValue>.Default.Equals(value, default!)) {
            AddError(errors, context);
        }
    }
}
