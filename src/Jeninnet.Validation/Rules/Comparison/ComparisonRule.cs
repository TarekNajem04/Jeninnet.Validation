//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Provides a base class for comparable type comparison rules.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="threshold">The threshold to compare against.</param>
/// <param name="message">The custom validation message, if any.</param>
/// <param name="messageKey">The key for the validation message.</param>
public abstract class ComparisonRule<T, TValue>(IPropertyAccessor<T, TValue> accessor, TValue threshold, string? message, string? messageKey = null)
    : ComparisonRuleBase<T, TValue>(accessor, threshold, message, messageKey) {
    /// <inheritdoc/>
    /// <param name="value">The value to compare.</param>
    /// <param name="threshold">The threshold to compare against.</param>
    protected sealed override bool Compare(TValue value, TValue threshold) =>
        IsValid(ValueComparison.Compare(value, threshold));

    /// <summary>
    /// Determines if the comparison result is valid.
    /// </summary>
    /// <param name="comparisonResult">The result of the comparison.</param>
    /// <returns><c>true</c> if the comparison result is valid; otherwise, <c>false</c>.</returns>
    protected abstract bool IsValid(int comparisonResult);
}
