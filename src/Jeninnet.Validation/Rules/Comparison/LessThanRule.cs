//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a numeric member is less than a specified threshold.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="threshold">The threshold value.</param>
/// <param name="message">The custom error message, if any.</param>
public sealed class LessThanRule<T, TValue>(
    IPropertyAccessor<T, TValue> accessor,
    TValue threshold,
    string? message
) : ComparisonRuleBase<T, TValue>(accessor, threshold, message, ValidationMessageKeys.LESS_THAN) {
    /// <inheritdoc />
    protected override bool Compare(TValue value, TValue threshold)
        => ValueComparison.Compare(value, threshold) < 0;
}
