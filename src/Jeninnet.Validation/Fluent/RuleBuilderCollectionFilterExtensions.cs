//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Fluent element filters for <c>ForEach</c> collection rules.
/// </summary>
public static class RuleBuilderCollectionFilterExtensions {
    // -----------------------------------------------------------------------
    // Where
    // -----------------------------------------------------------------------

    /// <summary>
    /// Filters the elements validated by the immediately preceding <c>ForEach</c> rule. Only
    /// elements satisfying <paramref name="predicate"/> reach the child validator; original
    /// indices are preserved. The filter runs on both the synchronous and asynchronous paths.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The element predicate.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No <c>ForEach</c> rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, IEnumerable<TElement>?> Where<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder,
        Func<TElement, bool> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        ApplyFilter(builder, predicate, asyncFilter: null);
        return builder;
    }

    /// <summary>
    /// Filters the elements validated by the immediately preceding <c>ForEach</c> rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The element predicate.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No <c>ForEach</c> rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, ICollection<TElement>?> Where<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder,
        Func<TElement, bool> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        ApplyFilter(builder, predicate, asyncFilter: null);
        return builder;
    }

    /// <summary>
    /// Filters the elements validated by the immediately preceding <c>ForEach</c> rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The element predicate.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No <c>ForEach</c> rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, IList<TElement>?> Where<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder,
        Func<TElement, bool> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        ApplyFilter(builder, predicate, asyncFilter: null);
        return builder;
    }

    /// <summary>
    /// Filters the elements validated by the immediately preceding <c>ForEach</c> rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The element predicate.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No <c>ForEach</c> rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, List<TElement>?> Where<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder,
        Func<TElement, bool> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        ApplyFilter(builder, predicate, asyncFilter: null);
        return builder;
    }

    /// <summary>
    /// Filters the elements validated by the immediately preceding <c>ForEach</c> rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The element predicate.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No <c>ForEach</c> rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, TElement[]?> Where<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder,
        Func<TElement, bool> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        ApplyFilter(builder, predicate, asyncFilter: null);
        return builder;
    }

    // -----------------------------------------------------------------------
    // WhereAsync
    // -----------------------------------------------------------------------

    /// <summary>
    /// Filters the elements validated by the immediately preceding <c>ForEach</c> rule using an
    /// asynchronous predicate. It runs on the asynchronous path only; when a synchronous filter is
    /// also configured, both must pass.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The asynchronous element predicate.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No <c>ForEach</c> rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, IEnumerable<TElement>?> WhereAsync<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder,
        Func<TElement, CancellationToken, ValueTask<bool>> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        ApplyFilter(builder, filter: null, predicate);
        return builder;
    }

    /// <summary>
    /// Filters the elements validated by the immediately preceding <c>ForEach</c> rule using an
    /// asynchronous predicate.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The asynchronous element predicate.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No <c>ForEach</c> rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, ICollection<TElement>?> WhereAsync<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder,
        Func<TElement, CancellationToken, ValueTask<bool>> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        ApplyFilter(builder, filter: null, predicate);
        return builder;
    }

    /// <summary>
    /// Filters the elements validated by the immediately preceding <c>ForEach</c> rule using an
    /// asynchronous predicate.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The asynchronous element predicate.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No <c>ForEach</c> rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, IList<TElement>?> WhereAsync<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder,
        Func<TElement, CancellationToken, ValueTask<bool>> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        ApplyFilter(builder, filter: null, predicate);
        return builder;
    }

    /// <summary>
    /// Filters the elements validated by the immediately preceding <c>ForEach</c> rule using an
    /// asynchronous predicate.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The asynchronous element predicate.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No <c>ForEach</c> rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, List<TElement>?> WhereAsync<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder,
        Func<TElement, CancellationToken, ValueTask<bool>> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        ApplyFilter(builder, filter: null, predicate);
        return builder;
    }

    /// <summary>
    /// Filters the elements validated by the immediately preceding <c>ForEach</c> rule using an
    /// asynchronous predicate.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The asynchronous element predicate.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No <c>ForEach</c> rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, TElement[]?> WhereAsync<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder,
        Func<TElement, CancellationToken, ValueTask<bool>> predicate) {
        ArgumentNullException.ThrowIfNull(predicate);

        ApplyFilter(builder, filter: null, predicate);
        return builder;
    }

    private static void ApplyFilter<T, TCollection, TElement>(
        IRuleBuilder<T, TCollection> builder,
        Func<TElement, bool>? filter,
        Func<TElement, CancellationToken, ValueTask<bool>>? asyncFilter) {
        ArgumentNullException.ThrowIfNull(builder);

        if(builder is not RuleBuilder<T, TCollection> concrete) {
            throw new NotSupportedException(
                "Collection filters require the built-in fluent RuleBuilder<T, TProperty> implementation.");
        }

        concrete.ApplyForEachFilter(filter, asyncFilter);
    }
}
