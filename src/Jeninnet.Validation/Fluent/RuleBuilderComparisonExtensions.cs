//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for property comparison rules.
/// </summary>
public static class RuleBuilderComparisonExtensions {
    // =======================================================================
    //  EQUAL
    // =======================================================================

    /// <summary>
    /// Adds an equality validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="threshold">The expected value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Equal<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue threshold,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new EqualRule<T, TValue>(builder.Accessor, threshold, message));
        return builder;
    }

    /// <summary>
    /// Adds an equality validation rule with an explicit equality comparer.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="threshold">The expected value.</param>
    /// <param name="comparer">The equality comparer.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Equal<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue threshold,
        IEqualityComparer<TValue> comparer,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(comparer);

        builder.AddRule(new EqualRule<T, TValue>(
            builder.Accessor,
            threshold,
            message,
            comparer: comparer));
        return builder;
    }

    // =======================================================================
    //  NOT EQUAL
    // =======================================================================

    /// <summary>
    /// Adds an inequality validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="threshold">The value to compare against.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> NotEqual<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue threshold,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new NotEqualRule<T, TValue>(builder.Accessor, threshold, message));
        return builder;
    }

    /// <summary>
    /// Adds an inequality validation rule with an explicit equality comparer.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="threshold">The value to compare against.</param>
    /// <param name="comparer">The equality comparer.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> NotEqual<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue threshold,
        IEqualityComparer<TValue> comparer,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(comparer);

        builder.AddRule(new NotEqualRule<T, TValue>(
            builder.Accessor,
            threshold,
            message,
            comparer: comparer));
        return builder;
    }

    // =======================================================================
    //  GREATER THAN
    // =======================================================================

    /// <summary>
    /// Adds a greater-than validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="threshold">The minimum exclusive value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> GreaterThan<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue threshold,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new GreaterThanRule<T, TValue>(builder.Accessor, threshold, message));
        return builder;
    }

    // =======================================================================
    //  GREATER THAN OR EQUAL
    // =======================================================================

    /// <summary>
    /// Adds a greater-than-or-equal validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="threshold">The minimum inclusive value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> GreaterThanOrEqual<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue threshold,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new GreaterThanOrEqualRule<T, TValue>(builder.Accessor, threshold, message));
        return builder;
    }

    // =======================================================================
    //  LESS THAN
    // =======================================================================

    /// <summary>
    /// Adds a less-than validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="threshold">The maximum exclusive value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> LessThan<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue threshold,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new LessThanRule<T, TValue>(builder.Accessor, threshold, message));
        return builder;
    }

    // =======================================================================
    //  LESS THAN OR EQUAL
    // =======================================================================

    /// <summary>
    /// Adds a less-than-or-equal validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="threshold">The maximum inclusive value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> LessThanOrEqual<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue threshold,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new LessThanOrEqualRule<T, TValue>(builder.Accessor, threshold, message));
        return builder;
    }

    // =======================================================================
    //  BETWEEN
    // =======================================================================

    /// <summary>
    /// Adds an inclusive between validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="min">The minimum inclusive value.</param>
    /// <param name="max">The maximum inclusive value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static IRuleBuilder<T, TValue> InclusiveBetween<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue min,
        TValue max,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new InclusiveBetweenRule<T, TValue>(builder.Accessor, min, max, message));
        return builder;
    }

    /// <summary>
    /// Adds an exclusive between validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="min">The minimum exclusive value.</param>
    /// <param name="max">The maximum exclusive value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than or equal to <paramref name="max"/>.</exception>
    public static IRuleBuilder<T, TValue> ExclusiveBetween<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue min,
        TValue max,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new ExclusiveBetweenRule<T, TValue>(builder.Accessor, min, max, message));
        return builder;
    }

    // =======================================================================
    //  RANGE
    // =======================================================================

    /// <summary>
    /// Adds a range validation rule for struct values.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The struct type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="min">The minimum inclusive value.</param>
    /// <param name="max">The maximum inclusive value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static IRuleBuilder<T, TValue> Range<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue min,
        TValue max,
        string? message = null
    ) where TValue : struct, IComparable<TValue> {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new RangeRule<T, TValue>(builder.Accessor, min, max, message));
        return builder;
    }

    // =======================================================================
    //  SCALE PRECISION
    // =======================================================================

    /// <summary>
    /// Adds a scale precision validation rule for numeric values.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="precision">The maximum number of digits.</param>
    /// <param name="scale">The maximum number of decimal places.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> ScalePrecision<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        int precision,
        int scale,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new ScalePrecisionRule<T, TValue>(builder.Accessor, precision, scale, message));
        return builder;
    }

    /// <summary>
    /// Adds a precision/scale validation rule that can ignore trailing fractional zeros.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="precision">The maximum number of digits.</param>
    /// <param name="scale">The number of digits after the decimal point.</param>
    /// <param name="ignoreTrailingZeros">Whether trailing fractional zeros are ignored.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> ScalePrecision<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        int precision,
        int scale,
        bool ignoreTrailingZeros,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new ScalePrecisionRule<T, TValue>(
            builder.Accessor,
            precision,
            scale,
            message,
            ignoreTrailingZeros: ignoreTrailingZeros));
        return builder;
    }
}
