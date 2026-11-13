//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for collection validation rules.
/// </summary>
public static class RuleBuilderCollectionExtensions {
    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="syncValidator">The synchronous validator for the elements.</param>
    /// <param name="asyncValidator">The asynchronous validator for the elements.</param>
    public static IRuleBuilder<T, IEnumerable<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder,
        ISyncValidator<TElement>? syncValidator,
        IAsyncValidator<TElement>? asyncValidator
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        var rule = new ForEachRule<T, TElement>(builder.Accessor, syncValidator, asyncValidator);

        if(syncValidator is not null || asyncValidator is null) {
            builder.AddRule(rule);
        }

        if(asyncValidator is not null) {
            builder.AddAsyncRule(rule);
        }

        return builder;
    }

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The validator for the elements.</param>
    public static IRuleBuilder<T, IEnumerable<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder,
        IValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(validator, null);
    }

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The synchronous validator for the elements.</param>
    public static IRuleBuilder<T, IEnumerable<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder,
        ISyncValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(validator, null);
    }

    /// <summary>
    /// Applies an async child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The asynchronous validator for the elements.</param>
    public static IRuleBuilder<T, IEnumerable<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, IEnumerable<TElement>?> builder,
        IAsyncValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(null, validator);
    }

    // =======================================================================
    // ICollection other collection-related extension methods here...
    // =======================================================================

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="syncValidator">The synchronous validator for the elements.</param>
    /// <param name="asyncValidator">The asynchronous validator for the elements.</param>
    public static IRuleBuilder<T, ICollection<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder,
        ISyncValidator<TElement>? syncValidator,
        IAsyncValidator<TElement>? asyncValidator
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        var rule = new ForEachRule<T, TElement>(builder.Accessor, syncValidator, asyncValidator);

        if(syncValidator is not null || asyncValidator is null) {
            builder.AddRule(rule);
        }

        if(asyncValidator is not null) {
            builder.AddAsyncRule(rule);
        }

        return builder;
    }

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The validator for the elements.</param>
    public static IRuleBuilder<T, ICollection<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder,
        IValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(validator, null);
    }

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The synchronous validator for the elements.</param>
    public static IRuleBuilder<T, ICollection<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder,
        ISyncValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(validator, null);
    }

    /// <summary>
    /// Applies an async child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The asynchronous validator for the elements.</param>
    public static IRuleBuilder<T, ICollection<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, ICollection<TElement>?> builder,
        IAsyncValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(null, validator);
    }

    // =======================================================================
    // IList other collection-related extension methods here...
    // =======================================================================

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="syncValidator">The synchronous validator for the elements.</param>
    /// <param name="asyncValidator">The asynchronous validator for the elements.</param>
    public static IRuleBuilder<T, IList<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder,
        ISyncValidator<TElement>? syncValidator,
        IAsyncValidator<TElement>? asyncValidator
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        var rule = new ForEachRule<T, TElement>(builder.Accessor, syncValidator, asyncValidator);

        if(syncValidator is not null || asyncValidator is null) {
            builder.AddRule(rule);
        }

        if(asyncValidator is not null) {
            builder.AddAsyncRule(rule);
        }

        return builder;
    }

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The validator for the elements.</param>
    public static IRuleBuilder<T, IList<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder,
        IValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(validator, null);
    }

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The synchronous validator for the elements.</param>
    public static IRuleBuilder<T, IList<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder,
        ISyncValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(validator, null);
    }

    /// <summary>
    /// Applies an async child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The asynchronous validator for the elements.</param>
    public static IRuleBuilder<T, IList<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, IList<TElement>?> builder,
        IAsyncValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(null, validator);
    }

    // =======================================================================
    // List other collection-related extension methods here...
    // =======================================================================

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="syncValidator">The synchronous validator for the elements.</param>
    /// <param name="asyncValidator">The asynchronous validator for the elements.</param>
    public static IRuleBuilder<T, List<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder,
        ISyncValidator<TElement>? syncValidator,
        IAsyncValidator<TElement>? asyncValidator
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        var rule = new ForEachRule<T, TElement>(builder.Accessor, syncValidator, asyncValidator);

        if(syncValidator is not null || asyncValidator is null) {
            builder.AddRule(rule);
        }

        if(asyncValidator is not null) {
            builder.AddAsyncRule(rule);
        }

        return builder;
    }

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The validator for the elements.</param>
    public static IRuleBuilder<T, List<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder,
        IValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(validator, null);
    }

    /// <summary>
    /// Applies a child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The synchronous validator for the elements.</param>
    public static IRuleBuilder<T, List<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder,
        ISyncValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(validator, null);
    }

    /// <summary>
    /// Applies an async child validator to each element of the collection.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the collection.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The asynchronous validator for the elements.</param>
    public static IRuleBuilder<T, List<TElement>?> ForEach<T, TElement>(
        this IRuleBuilder<T, List<TElement>?> builder,
        IAsyncValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(null, validator);
    }

    // =======================================================================
    // Array overloads
    // =======================================================================

    /// <summary>
    /// Applies a child validator to each element of the array.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="syncValidator">The synchronous validator for the elements.</param>
    /// <param name="asyncValidator">The asynchronous validator for the elements.</param>
    public static IRuleBuilder<T, TElement[]?> ForEach<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder,
        ISyncValidator<TElement>? syncValidator,
        IAsyncValidator<TElement>? asyncValidator
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        var accessor = ValidatorFactory.Accessor<T, IEnumerable<TElement>?>(
            instance => builder.Accessor.Getter(instance),
            builder.Accessor.MemberName);
        var rule = new ForEachRule<T, TElement>(accessor, syncValidator, asyncValidator);

        if(syncValidator is not null || asyncValidator is null) {
            builder.AddRule(rule);
        }

        if(asyncValidator is not null) {
            builder.AddAsyncRule(rule);
        }

        return builder;
    }

    /// <summary>
    /// Applies a child validator to each element of the array.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The validator for the elements.</param>
    public static IRuleBuilder<T, TElement[]?> ForEach<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder,
        IValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(validator, null);
    }

    /// <summary>
    /// Applies a child validator to each element of the array.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The synchronous validator for the elements.</param>
    public static IRuleBuilder<T, TElement[]?> ForEach<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder,
        ISyncValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(validator, null);
    }

    /// <summary>
    /// Applies an async child validator to each element of the array.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The asynchronous validator for the elements.</param>
    public static IRuleBuilder<T, TElement[]?> ForEach<T, TElement>(
        this IRuleBuilder<T, TElement[]?> builder,
        IAsyncValidator<TElement> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ForEach(null, validator);
    }
}
