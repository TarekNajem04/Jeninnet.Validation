//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Fluent extensions for context-aware custom validation delegates (multi-error emission).
/// </summary>
public static class CustomRuleExtensions {
    /// <summary>
    /// Adds a synchronous custom validation rule. The delegate receives the member value, the
    /// validation context, and the error collector, and may add any number of errors.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The member value type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validation">The custom validation delegate.</param>
    /// <param name="message">The optional fallback message (used when the delegate throws).</param>
    /// <param name="messageKey">The optional fallback message key.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="validation"/> is null.</exception>
    public static IRuleBuilder<T, TValue> Custom<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        CustomValidation<T, TValue> validation,
        string? message = null,
        string? messageKey = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(validation);

        builder.AddRule(new CustomRule<T, TValue>(builder.Accessor, validation, message, messageKey));
        return builder;
    }

    /// <summary>
    /// Adds an asynchronous custom validation rule. The delegate receives the member value, the
    /// validation context, the error collector, and the cancellation token, and may add any number
    /// of errors. It runs on the asynchronous path only.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The member value type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validation">The custom asynchronous validation delegate.</param>
    /// <param name="message">The optional fallback message (used when the delegate throws).</param>
    /// <param name="messageKey">The optional fallback message key.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="validation"/> is null.</exception>
    public static IRuleBuilder<T, TValue> CustomAsync<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        CustomValidationAsync<T, TValue> validation,
        string? message = null,
        string? messageKey = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(validation);

        builder.AddAsyncRule(new CustomAsyncRule<T, TValue>(builder.Accessor, validation, message, messageKey));
        return builder;
    }
}
