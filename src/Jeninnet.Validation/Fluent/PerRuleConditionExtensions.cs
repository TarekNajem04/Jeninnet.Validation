//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Per-rule condition extensions for <see cref="IRuleBuilder{T, TProperty}"/>.
/// </summary>
public static class PerRuleConditionExtensions {
    /// <summary>
    /// Applies a synchronous condition to the rule that was just configured (or to all rules
    /// already configured for the member when <paramref name="applyTo"/> is
    /// <see cref="ApplyConditionTo.AllValidators"/>).
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The member type.</typeparam>
    /// <param name="builder">The rule builder (must be the built-in <see cref="RuleBuilder{T, TProperty}"/>).</param>
    /// <param name="condition">The condition that must be satisfied for the rule(s) to execute.</param>
    /// <param name="applyTo">The condition scope (defaults to the current rule).</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="condition"/> is null.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, TProperty> When<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        Func<T, bool> condition,
        ApplyConditionTo applyTo = ApplyConditionTo.CurrentValidator) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(condition);

        return ApplyPerRuleCondition(builder, condition, asyncCondition: null, applyTo);
    }

    /// <summary>
    /// Applies an asynchronous condition to the rule that was just configured (or to all rules
    /// already configured for the member). Async conditions run on the asynchronous path only;
    /// synchronous validation skips the condition (and its rule).
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The member type.</typeparam>
    /// <param name="builder">The rule builder (must be the built-in <see cref="RuleBuilder{T, TProperty}"/>).</param>
    /// <param name="condition">The asynchronous condition.</param>
    /// <param name="applyTo">The condition scope (defaults to the current rule).</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="condition"/> is null.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, TProperty> WhenAsync<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        Func<T, CancellationToken, Task<bool>> condition,
        ApplyConditionTo applyTo = ApplyConditionTo.CurrentValidator) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(condition);

        return ApplyPerRuleCondition(builder, syncCondition: null, condition, applyTo);
    }

    /// <summary>
    /// Applies the inverse of a synchronous condition to the rule that was just configured (or to
    /// all rules already configured for the member).
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The member type.</typeparam>
    /// <param name="builder">The rule builder (must be the built-in <see cref="RuleBuilder{T, TProperty}"/>).</param>
    /// <param name="condition">The condition that must not be satisfied for the rule(s) to execute.</param>
    /// <param name="applyTo">The condition scope (defaults to the current rule).</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="condition"/> is null.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, TProperty> Unless<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        Func<T, bool> condition,
        ApplyConditionTo applyTo = ApplyConditionTo.CurrentValidator) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(condition);

        return ApplyPerRuleCondition(builder, instance => !condition(instance), asyncCondition: null, applyTo);
    }

    /// <summary>
    /// Applies the inverse of an asynchronous condition to the rule that was just configured (or to
    /// all rules already configured for the member). Async conditions run on the asynchronous path
    /// only.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The member type.</typeparam>
    /// <param name="builder">The rule builder (must be the built-in <see cref="RuleBuilder{T, TProperty}"/>).</param>
    /// <param name="condition">The asynchronous condition that must not be satisfied for the rule(s) to execute.</param>
    /// <param name="applyTo">The condition scope (defaults to the current rule).</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="condition"/> is null.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, TProperty> UnlessAsync<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        Func<T, CancellationToken, Task<bool>> condition,
        ApplyConditionTo applyTo = ApplyConditionTo.CurrentValidator) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(condition);

        return ApplyPerRuleCondition(
            builder,
            syncCondition: null,
            async (instance, token) => !await condition(instance, token).ConfigureAwait(false),
            applyTo);
    }

    private static IRuleBuilder<T, TProperty> ApplyPerRuleCondition<T, TProperty>(
        IRuleBuilder<T, TProperty> builder,
        Func<T, bool>? syncCondition,
        Func<T, CancellationToken, Task<bool>>? asyncCondition,
        ApplyConditionTo applyTo) {
        if(builder is not RuleBuilder<T, TProperty> concrete) {
            throw new NotSupportedException(
                "Per-rule conditions require the built-in fluent RuleBuilder<T, TProperty> implementation.");
        }

        return concrete.ApplyCondition(syncCondition, asyncCondition, applyTo);
    }
}
