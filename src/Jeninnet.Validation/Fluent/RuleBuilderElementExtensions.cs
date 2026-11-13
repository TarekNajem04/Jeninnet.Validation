//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Fluent element-rule and indexer extensions for collection members.
/// </summary>
public static class RuleBuilderElementExtensions {
    /// <summary>
    /// Applies value rules directly to each element of an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the rules applied to each element.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is null.
    /// </exception>
    public static IRuleBuilder<T, IEnumerable<TElement>?> ForEachElement<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder,
        Action<IRuleBuilder<TElement, TElement>> configure) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return AddElementRule<T, IEnumerable<TElement>?, TElement>(builder, builder.Accessor, configure);
    }

    /// <summary>
    /// Applies value rules directly to each element of an <see cref="ICollection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the rules applied to each element.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, ICollection<TElement>?> ForEachElement<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder,
        Action<IRuleBuilder<TElement, TElement>> configure) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return AddElementRule<T, ICollection<TElement>?, TElement>(
            builder,
            ConvertAccessor<T, ICollection<TElement>?, TElement>(builder),
            configure);
    }

    /// <summary>
    /// Applies value rules directly to each element of an <see cref="IList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the rules applied to each element.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, IList<TElement>?> ForEachElement<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder,
        Action<IRuleBuilder<TElement, TElement>> configure) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return AddElementRule<T, IList<TElement>?, TElement>(
            builder,
            ConvertAccessor<T, IList<TElement>?, TElement>(builder),
            configure);
    }

    /// <summary>
    /// Applies value rules directly to each element of a <see cref="List{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the rules applied to each element.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, List<TElement>?> ForEachElement<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder,
        Action<IRuleBuilder<TElement, TElement>> configure) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return AddElementRule<T, List<TElement>?, TElement>(
            builder,
            ConvertAccessor<T, List<TElement>?, TElement>(builder),
            configure);
    }

    /// <summary>
    /// Applies value rules directly to each element of an array.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the rules applied to each element.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TElement[]?> ForEachElement<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder,
        Action<IRuleBuilder<TElement, TElement>> configure) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return AddElementRule<T, TElement[]?, TElement>(
            builder,
            ConvertAccessor<T, TElement[]?, TElement>(builder),
            configure);
    }

    /// <summary>
    /// Overrides the indexer used by the immediately preceding <c>ForEach</c> or
    /// <c>ForEachElement</c> rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="indexer">
    /// A delegate receiving the parent instance and original element index; it returns the text
    /// inserted inside the brackets. The original index is still reported as
    /// <c>CollectionIndex</c>.
    /// </param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="indexer"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">No collection rule immediately precedes this call.</exception>
    public static IRuleBuilder<T, IEnumerable<TElement>?> OverrideIndexer<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder,
        Func<T, int, string> indexer) {
        ApplyIndexer<T, IEnumerable<TElement>?, TElement>(builder, indexer);
        return builder;
    }

    /// <summary>
    /// Overrides the indexer used by the immediately preceding collection rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="indexer">The indexer delegate.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, ICollection<TElement>?> OverrideIndexer<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder,
        Func<T, int, string> indexer) {
        ApplyIndexer<T, ICollection<TElement>?, TElement>(builder, indexer);
        return builder;
    }

    /// <summary>
    /// Overrides the indexer used by the immediately preceding collection rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="indexer">The indexer delegate.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, IList<TElement>?> OverrideIndexer<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder,
        Func<T, int, string> indexer) {
        ApplyIndexer<T, IList<TElement>?, TElement>(builder, indexer);
        return builder;
    }

    /// <summary>
    /// Overrides the indexer used by the immediately preceding collection rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="indexer">The indexer delegate.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, List<TElement>?> OverrideIndexer<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder,
        Func<T, int, string> indexer) {
        ApplyIndexer<T, List<TElement>?, TElement>(builder, indexer);
        return builder;
    }

    /// <summary>
    /// Overrides the indexer used by the immediately preceding collection rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="indexer">The indexer delegate.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TElement[]?> OverrideIndexer<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder,
        Func<T, int, string> indexer) {
        ApplyIndexer<T, TElement[]?, TElement>(builder, indexer);
        return builder;
    }

    private static IRuleBuilder<T, TCollection> AddElementRule<T, TCollection, TElement>(
        IRuleBuilder<T, TCollection> builder,
        IPropertyAccessor<T, IEnumerable<TElement>?> accessor,
        Action<IRuleBuilder<TElement, TElement>> configure) {
        var cascadeMode = builder is IRuleLevelCascadeModeProvider provider
            ? provider.RuleLevelCascadeMode
            : null;
        var elementBuilder = new ElementRuleBuilder<TElement>(cascadeMode);
        configure(elementBuilder);

        if(elementBuilder.SyncRules.Count is 0 && elementBuilder.AsyncRules.Count is 0) {
            return builder;
        }

        var rule = new ForEachElementRule<T, TElement>(
            accessor,
            elementBuilder.SyncRules,
            elementBuilder.AsyncRules,
            elementBuilder.CascadeMode);

        if(elementBuilder.SyncRules.Count > 0 || elementBuilder.AsyncRules.Count is 0) {
            builder.AddRule(rule);
        }

        if(elementBuilder.AsyncRules.Count > 0) {
            builder.AddAsyncRule(rule);
        }

        return builder;
    }

    private static IPropertyAccessor<T, IEnumerable<TElement>?> ConvertAccessor<T, TCollection, TElement>(
        IRuleBuilder<T, TCollection> builder) {
        ArgumentNullException.ThrowIfNull(builder);

        return ValidatorFactory.Accessor<T, IEnumerable<TElement>?>(
            instance => (IEnumerable<TElement>?)builder.Accessor.Getter(instance),
            builder.Accessor.MemberName);
    }

    private static void ApplyIndexer<T, TCollection, TElement>(
        IRuleBuilder<T, TCollection> builder,
        Func<T, int, string> indexer) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(indexer);

        if(builder is not RuleBuilder<T, TCollection> concrete) {
            throw new NotSupportedException(
                "OverrideIndexer requires the built-in fluent RuleBuilder<T, TProperty> implementation.");
        }

        concrete.ApplyForEachIndexer<TElement>(indexer);
    }
}
