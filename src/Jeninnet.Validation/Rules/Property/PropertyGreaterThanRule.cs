//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Represents a rule for validating that one property is greater than another.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <param name="left">The left property accessor.</param>
/// <param name="right">The right property accessor.</param>
/// <param name="message">The optional error message.</param>
/// <param name="messageKey">The message key for localization.</param>
public sealed class PropertyGreaterThanRule<T, TProperty>(
    IPropertyAccessor<T, TProperty> left,
    IPropertyAccessor<T, TProperty> right,
    string? message,
    string? messageKey = ValidationMessageKeys.GREATER_THAN
) : PropertyComparisonRuleBase<T, TProperty>(left, right, message, messageKey) {
    /// <inheritdoc />
    protected override bool IsValid(int comparisonResult) => comparisonResult > 0;
}
