//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Provides a strongly-typed base class for synchronous validation rules.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The optional message key.</param>
public abstract class SyncValidateRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> accessor,
    string? message = null,
    string? messageKey = null)
    : ValidateRuleBase<T, TProperty>(accessor, message, messageKey), ISyncValidationRule<T> {
    /// <inheritdoc />
    public virtual void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        var value = Accessor.GetValue(context.Instance);
        ValidateValue(value, context, errors);
    }

    /// <summary>
    /// Validates the property value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">The validation error collector.</param>
    protected abstract void ValidateValue(TProperty value, in ValidationContext<T> context, ValidationErrorCollector errors);
}
