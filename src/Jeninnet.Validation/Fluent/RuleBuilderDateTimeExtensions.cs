//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for configuring date and time validation rules.
/// </summary>
public static class RuleBuilderDateTimeExtensions {
    // =======================================================================
    //  Before
    // =======================================================================

    /// <summary>
    /// Adds a before-date validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The date/time type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="threshold">The maximum exclusive date/time value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Before<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue threshold,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new BeforeRule<T, TValue>(builder.Accessor, threshold, message));
        return builder;
    }

    // =======================================================================
    //  After
    // =======================================================================

    /// <summary>
    /// Adds an after-date validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The date/time type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="threshold">The minimum exclusive date/time value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> After<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        TValue threshold,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new AfterRule<T, TValue>(builder.Accessor, threshold, message));
        return builder;
    }
}
