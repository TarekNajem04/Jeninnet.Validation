//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Ensures that a numeric member is within the specified exclusive range between  <![CDATA[min > value < max]]>.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The property/value type.</typeparam>
public sealed class ExclusiveBetweenRule<T, TValue> : BetweenRule<T, TValue> {
    /// <summary>
    /// Initializes a new instance of the <see cref="ExclusiveBetweenRule{T, TValue}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor to extract the value.</param>
    /// <param name="min">The minimum exclusive bound.</param>
    /// <param name="max">The maximum exclusive bound.</param>
    /// <param name="message">The custom validation message, if any.</param>
    /// <param name="messageKey">The key for the validation message.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="min"/> is greater than or equal to <paramref name="max"/>.
    /// </exception>
    public ExclusiveBetweenRule(
        IPropertyAccessor<T, TValue> accessor,
        TValue min,
        TValue max,
        string? message,
        string? messageKey = ValidationMessageKeys.EXCLUSIVE_BETWEEN
    ) : base(accessor, min, max, message, messageKey) => RangeBounds.EnsureOrdered(min, max, exclusive: true);

    /// <summary>
    /// Determines if the comparison results are valid for the range.
    /// </summary>
    /// <param name="minComparisonResult">The result of the comparison with the minimum bound.</param>
    /// <param name="maxComparisonResult">The result of the comparison with the maximum bound.</param>
    /// <returns><c>true</c> if the range is valid; otherwise, <c>false</c>.</returns>
    protected override bool IsValid(int minComparisonResult, int maxComparisonResult) => minComparisonResult > 0 && maxComparisonResult < 0;
}
