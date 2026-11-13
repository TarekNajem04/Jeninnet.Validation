//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a numeric member is within the specified inclusive range between <![CDATA[min <= value <= max]]>.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
/// <param name="accessor">The property accessor to extract the value.</param>
/// <param name="min">The minimum value.</param>
/// <param name="max">The maximum value.</param>
/// <param name="message">The custom error message, if any.</param>
/// <param name="messageKey">The key for the validation message.</param>
public sealed class InclusiveBetweenRule<T, TValue>(
    IPropertyAccessor<T, TValue> accessor,
    TValue min,
    TValue max,
    string? message,
    string? messageKey = ValidationMessageKeys.INCLUSIVE_BETWEEN
) : BetweenRule<T, TValue>(accessor, min, max, message, messageKey) {
    /// <inheritdoc />
    protected override bool IsValid(int minComparisonResult, int maxComparisonResult) => minComparisonResult >= 0 && maxComparisonResult <= 0;
}
