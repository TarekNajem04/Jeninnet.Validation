//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for runtime-type validation of base types and interfaces.
/// </summary>
public static class RuleBuilderInheritanceExtensions {
    /// <summary>
    /// Applies a validator selected by the actual runtime type of the property.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The declared base type or interface.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the explicit runtime-type map.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <remarks>
    /// Only exact runtime types are dispatched. A runtime type that is not registered is skipped.
    /// </remarks>
    public static IRuleBuilder<T, TProperty?> SetInheritanceValidator<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder,
        Action<InheritanceValidatorMap<TProperty>> configure)
        where TProperty : class {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.SetValidator(CreateValidator(configure));
    }

    /// <summary>
    /// Applies a runtime-type validator to each element of an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The declared element base type or interface.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the explicit runtime-type map.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, IEnumerable<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder,
        Action<InheritanceValidatorMap<TElement>> configure)
        where TElement : class {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ForEach(CreateValidator(configure));
    }

    /// <summary>
    /// Applies a runtime-type validator to each element of an <see cref="ICollection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The declared element base type or interface.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the explicit runtime-type map.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, ICollection<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder,
        Action<InheritanceValidatorMap<TElement>> configure)
        where TElement : class {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ForEach(CreateValidator(configure));
    }

    /// <summary>
    /// Applies a runtime-type validator to each element of an <see cref="IList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The declared element base type or interface.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the explicit runtime-type map.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, IList<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder,
        Action<InheritanceValidatorMap<TElement>> configure)
        where TElement : class {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ForEach(CreateValidator(configure));
    }

    /// <summary>
    /// Applies a runtime-type validator to each element of a <see cref="List{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The declared element base type or interface.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the explicit runtime-type map.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, List<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder,
        Action<InheritanceValidatorMap<TElement>> configure)
        where TElement : class {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ForEach(CreateValidator(configure));
    }

    /// <summary>
    /// Applies a runtime-type validator to each element of an array.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The declared element base type or interface.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">Configures the explicit runtime-type map.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TElement[]?> ForEach<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder,
        Action<InheritanceValidatorMap<TElement>> configure)
        where TElement : class {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ForEach(CreateValidator(configure));
    }

    private static InheritanceValidator<TElement> CreateValidator<TElement>(
        Action<InheritanceValidatorMap<TElement>> configure)
        where TElement : class {
        var map = new InheritanceValidatorMap<TElement>();
        configure(map);
        return new InheritanceValidator<TElement>(map);
    }
}
