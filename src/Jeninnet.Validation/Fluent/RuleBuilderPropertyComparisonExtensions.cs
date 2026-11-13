//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for property-to-property comparison rules.
/// </summary>
public static class RuleBuilderPropertyComparisonExtensions {
    // -----------------------------------------------------------------------
    // EQUAL
    // -----------------------------------------------------------------------

    /// <summary>
    /// Adds a property equality validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="right">The property accessor for comparison.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TProperty> Equal<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        IPropertyAccessor<T, TProperty> right,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new PropertyEqualRule<T, TProperty>(builder.Accessor, right, message));
        return builder;
    }

    // -----------------------------------------------------------------------
    // NOT EQUAL
    // -----------------------------------------------------------------------

    /// <summary>
    /// Adds a property inequality validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="right">The property accessor for comparison.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TProperty> NotEqual<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        IPropertyAccessor<T, TProperty> right,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new PropertyNotEqualRule<T, TProperty>(builder.Accessor, right, message));
        return builder;
    }

    // =======================================================================
    //  GREATER THAN
    // =======================================================================

    /// <summary>
    /// Adds a property greater-than validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="right">The property accessor for comparison.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TProperty> GreaterThan<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        IPropertyAccessor<T, TProperty> right,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new PropertyGreaterThanRule<T, TProperty>(builder.Accessor, right, message));
        return builder;
    }

    // =======================================================================
    //  GREATER THAN OR EQUAL
    // =======================================================================

    /// <summary>
    /// Adds a property greater-than-or-equal validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="right">The property accessor for comparison.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TProperty> GreaterThanOrEqual<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        IPropertyAccessor<T, TProperty> right,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new PropertyGreaterThanOrEqualRule<T, TProperty>(builder.Accessor, right, message));
        return builder;
    }

    // =======================================================================
    //  LESS THAN
    // =======================================================================

    /// <summary>
    /// Adds a property less-than validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="right">The property accessor for comparison.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TProperty> LessThan<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        IPropertyAccessor<T, TProperty> right,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new PropertyLessThanRule<T, TProperty>(builder.Accessor, right, message));
        return builder;
    }

    // =======================================================================
    //  LESS THAN OR EQUAL
    // =======================================================================

    /// <summary>
    /// Adds a property less-than-or-equal validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="right">The property accessor for comparison.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TProperty> LessThanOrEqual<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        IPropertyAccessor<T, TProperty> right,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new PropertyLessThanOrEqualRule<T, TProperty>(builder.Accessor, right, message));
        return builder;
    }
}
