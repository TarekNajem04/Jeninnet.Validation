//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Fluent extension for the multiple-of numeric rule.
/// </summary>
public static class RuleBuilderMultipleOfExtensions {
    /// <summary>
    /// Adds a rule that requires the numeric member to be a multiple of
    /// <paramref name="divisor"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The member type.</typeparam>
    /// <typeparam name="TDivisor">The divisor type (integer, floating point, decimal, or string).</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="divisor">The positive divisor.</param>
    /// <param name="message">The optional error message.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">The divisor is not numeric.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The divisor is zero or negative.</exception>
    public static IRuleBuilder<T, TValue> MultipleOf<T, TValue, TDivisor>(
        this IRuleBuilder<T, TValue> builder,
        TDivisor divisor,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        var positiveDivisor = NumericValueConversion.ToPositiveDivisor(divisor);
        builder.AddRule(new MultipleOfRule<T, TValue>(builder.Accessor, positiveDivisor, message));
        return builder;
    }
}
