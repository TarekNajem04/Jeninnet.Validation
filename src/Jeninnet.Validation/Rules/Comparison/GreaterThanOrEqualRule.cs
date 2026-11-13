//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a member is greater than or equal to the specified threshold.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="threshold">The threshold to compare against.</param>
/// <param name="message">The custom validation message, if any.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class GreaterThanOrEqualRule<T, TValue>(
    IPropertyAccessor<T, TValue> accessor,
    TValue threshold,
    string? message,
    string? messageKey = ValidationMessageKeys.GREATER_THAN_OR_EQUAL
) : ComparisonRuleBase<T, TValue>(accessor, threshold, message, messageKey) {
    /// <inheritdoc />
    protected override bool Compare(TValue value, TValue threshold)
        => ValueComparison.Compare(value, threshold) >= 0;
}
