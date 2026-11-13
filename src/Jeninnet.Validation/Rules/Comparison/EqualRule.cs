//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a member is equal to a specified expected value.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="expected">The expected value.</param>
/// <param name="message">The custom validation message, if any.</param>
/// <param name="messageKey">The key for the validation message.</param>
/// <param name="comparer">The optional equality comparer; defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
public sealed class EqualRule<T, TValue>(
    IPropertyAccessor<T, TValue> accessor,
    TValue expected,
    string? message,
    string? messageKey = ValidationMessageKeys.EQUAL,
    IEqualityComparer<TValue>? comparer = null
) : ComparisonRuleBase<T, TValue>(accessor, expected, message, messageKey) {
    private readonly IEqualityComparer<TValue> _comparer = comparer ?? EqualityComparer<TValue>.Default;

    /// <summary>
    /// Compares the specified value with the threshold.
    /// </summary>
    /// <param name="value">The value to compare.</param>
    /// <param name="threshold">The threshold to compare against.</param>
    /// <returns><c>true</c> if the comparison is valid; otherwise, <c>false</c>.</returns>
    protected override bool Compare(TValue value, TValue threshold) =>
        _comparer.Equals(value, threshold);
}
