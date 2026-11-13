//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Fluent extensions for the unique-items collection rule.
/// </summary>
public static class RuleBuilderUniqueExtensions {
    /// <summary>
    /// Adds a rule that requires every element of an <see cref="IEnumerable{T}"/> member to be unique.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, IEnumerable<TElement>?> Unique<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder) =>
        AddUnique<T, IEnumerable<TElement>?, TElement>(builder, comparer: null);

    /// <summary>
    /// Adds a rule that requires every element of an <see cref="IEnumerable{T}"/> member to be
    /// unique using the supplied comparer.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="comparer">The element comparer.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, IEnumerable<TElement>?> Unique<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder,
        IEqualityComparer<TElement>? comparer) =>
        AddUnique<T, IEnumerable<TElement>?, TElement>(builder, comparer);

    /// <summary>
    /// Adds a rule that requires every element of an <see cref="ICollection{T}"/> member to be unique.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, ICollection<TElement>?> Unique<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder) =>
        AddUnique<T, ICollection<TElement>?, TElement>(builder, comparer: null);

    /// <summary>
    /// Adds a rule that requires every element of an <see cref="ICollection{T}"/> member to be
    /// unique using the supplied comparer.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="comparer">The element comparer.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, ICollection<TElement>?> Unique<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder,
        IEqualityComparer<TElement>? comparer) =>
        AddUnique<T, ICollection<TElement>?, TElement>(builder, comparer);

    /// <summary>
    /// Adds a rule that requires every element of an <see cref="IList{T}"/> member to be unique.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, IList<TElement>?> Unique<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder) =>
        AddUnique<T, IList<TElement>?, TElement>(builder, comparer: null);

    /// <summary>
    /// Adds a rule that requires every element of an <see cref="IList{T}"/> member to be unique
    /// using the supplied comparer.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="comparer">The element comparer.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, IList<TElement>?> Unique<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder,
        IEqualityComparer<TElement>? comparer) =>
        AddUnique<T, IList<TElement>?, TElement>(builder, comparer);

    /// <summary>
    /// Adds a rule that requires every element of a <see cref="List{T}"/> member to be unique.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, List<TElement>?> Unique<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder) =>
        AddUnique<T, List<TElement>?, TElement>(builder, comparer: null);

    /// <summary>
    /// Adds a rule that requires every element of a <see cref="List{T}"/> member to be unique
    /// using the supplied comparer.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="comparer">The element comparer.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, List<TElement>?> Unique<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder,
        IEqualityComparer<TElement>? comparer) =>
        AddUnique<T, List<TElement>?, TElement>(builder, comparer);

    /// <summary>
    /// Adds a rule that requires every element of an array member to be unique.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, TElement[]?> Unique<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder) =>
        AddUnique<T, TElement[]?, TElement>(builder, comparer: null);

    /// <summary>
    /// Adds a rule that requires every element of an array member to be unique using the supplied
    /// comparer.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="comparer">The element comparer.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
    public static IRuleBuilder<T, TElement[]?> Unique<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder,
        IEqualityComparer<TElement>? comparer) =>
        AddUnique<T, TElement[]?, TElement>(builder, comparer);

    private static IRuleBuilder<T, TCollection> AddUnique<T, TCollection, TElement>(
        IRuleBuilder<T, TCollection> builder,
        IEqualityComparer<TElement>? comparer) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new UniqueItemsRule<T, TElement>(
            ValidatorFactory.Accessor<T, IEnumerable<TElement>?>(
                instance => (IEnumerable<TElement>?)(object?)builder.Accessor.Getter(instance),
                builder.Accessor.MemberName),
            comparer: comparer));

        return builder;
    }
}
