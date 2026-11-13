//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Fluent extensions for collection cardinality rules.
/// </summary>
public static class RuleBuilderCountExtensions {
    /// <summary>
    /// Adds a rule that requires the collection member to contain between
    /// <paramref name="min"/> and <paramref name="max"/> elements (inclusive).
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The collection property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="min">The minimum number of elements.</param>
    /// <param name="max">The maximum number of elements.</param>
    /// <param name="message">The optional error message.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> is negative or <paramref name="max"/> is less than <paramref name="min"/>.</exception>
    public static IRuleBuilder<T, TProperty> Count<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        int min,
        int max,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegative(min);
        ArgumentOutOfRangeException.ThrowIfLessThan(max, min);

        builder.AddRule(new CountRule<T, TProperty>(builder.Accessor, min, max, message));
        return builder;
    }

    /// <summary>
    /// Adds a rule that requires the collection member to contain at least
    /// <paramref name="min"/> elements.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The collection property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="min">The minimum number of elements.</param>
    /// <param name="message">The optional error message.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> is negative.</exception>
    public static IRuleBuilder<T, TProperty> MinCount<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        int min,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegative(min);

        builder.AddRule(new MinCountRule<T, TProperty>(builder.Accessor, min, message));
        return builder;
    }

    /// <summary>
    /// Adds a rule that requires the collection member to contain at most
    /// <paramref name="max"/> elements.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The collection property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="max">The maximum number of elements.</param>
    /// <param name="message">The optional error message.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="max"/> is negative.</exception>
    public static IRuleBuilder<T, TProperty> MaxCount<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        int max,
        string? message = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegative(max);

        builder.AddRule(new MaxCountRule<T, TProperty>(builder.Accessor, max, message));
        return builder;
    }
}
