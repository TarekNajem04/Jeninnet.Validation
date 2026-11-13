//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for common validation rules.
/// </summary>
public static class RuleBuilderCommonExtensions {
    // -----------------------------------------------------------------------
    // NOT NULL
    // -----------------------------------------------------------------------

    /// <summary>
    /// Adds a not-null validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> NotNull<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new NotNullRule<T, TValue>(builder.Accessor, message));
        return builder;
    }

    // -----------------------------------------------------------------------
    // NOT EMPTY
    // -----------------------------------------------------------------------

    /// <summary>
    /// Adds a not-empty validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> NotEmpty<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new NotEmptyRule<T, TValue>(builder.Accessor, message));
        return builder;
    }

    // -----------------------------------------------------------------------
    // NOT DEFAULT
    // -----------------------------------------------------------------------

    /// <summary>
    /// Adds a not-default validation rule. Unlike <see cref="NotEmpty{T,TValue}"/>,
    /// this rule treats <c>0</c>, <c>false</c>, <c>Guid.Empty</c>, and a default
    /// <see cref="DateTime"/> as failures.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> NotDefault<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new NotDefaultRule<T, TValue>(builder.Accessor, message));
        return builder;
    }

    // -----------------------------------------------------------------------
    // MUST (Predicate)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Adds a synchronous predicate-based validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The predicate that returns <see langword="true"/> if valid.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Must<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        Func<TValue, bool> predicate,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new MustRule<T, TValue>(builder.Accessor, predicate, message));
        return builder;
    }

    /// <summary>
    /// Adds an asynchronous predicate-based validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="predicate">The async predicate that returns <see langword="true"/> if valid.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Must<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        Func<TValue, CancellationToken, ValueTask<bool>> predicate,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddAsyncRule(new MustAsyncRule<T, TValue>(builder.Accessor, predicate, message));
        return builder;
    }

    /// <summary>
    /// Adds an enum validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The enum type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Enum<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new EnumRule<T, TValue>(builder.Accessor, typeof(TValue), message));
        return builder;
    }

    /// <summary>
    /// Adds an inclusion validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="allowedValues">The set of allowed values.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> In<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        IEnumerable<TValue> allowedValues,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new InRule<T, TValue>(builder.Accessor, allowedValues, message));
        return builder;
    }

    /// <summary>
    /// Adds an exclusion validation rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="disallowedValues">The set of disallowed values.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> NotIn<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        IEnumerable<TValue> disallowedValues,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new NotInRule<T, TValue>(builder.Accessor, disallowedValues, message));
        return builder;
    }

    // -----------------------------------------------------------------------
    // NULL
    // -----------------------------------------------------------------------

    /// <summary>
    /// Adds a null validation rule. Validates that the value is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Null<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new NullRule<T, TValue>(builder.Accessor, message));
        return builder;
    }

    // -----------------------------------------------------------------------
    // EMPTY
    // -----------------------------------------------------------------------

    /// <summary>
    /// Adds an empty validation rule. Validates that the value is empty.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> Empty<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new EmptyRule<T, TValue>(builder.Accessor, message));
        return builder;
    }

    // -----------------------------------------------------------------------
    // IS ENUM NAME
    // -----------------------------------------------------------------------

    /// <summary>
    /// Adds an is-enum-name validation rule. Validates that the string value is a valid name in the specified enum type.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="enumType">The enum type to check against.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> IsEnumName<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        Type enumType,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new IsEnumNameRule<T, TValue>(builder.Accessor, enumType, message));
        return builder;
    }

    /// <summary>
    /// Adds an is-enum-name validation rule with case-sensitivity option.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The type of the value being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="enumType">The enum type to check against.</param>
    /// <param name="caseSensitive">Whether the comparison is case-sensitive.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static IRuleBuilder<T, TValue> IsEnumName<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        Type enumType,
        bool caseSensitive,
        string? message = null
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddRule(new IsEnumNameRule<T, TValue>(builder.Accessor, enumType, caseSensitive, message));
        return builder;
    }
}
