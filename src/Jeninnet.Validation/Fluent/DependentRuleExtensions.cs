//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Fluent extensions for dependent rules.
/// </summary>
public static class DependentRuleExtensions {
    /// <summary>
    /// Adds a dependent block: the rules configured by <paramref name="configure"/> run only when
    /// the rule immediately preceding this call produced no errors. The parent runs first, then the
    /// dependent rules in declaration order.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The parent member type.</typeparam>
    /// <param name="builder">The rule builder (must be the built-in <see cref="RuleBuilder{T, TProperty}"/>).</param>
    /// <param name="configure">Configures the dependent rules on a dedicated sub-builder.</param>
    /// <returns>The same rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">No rule immediately precedes this call.</exception>
    /// <exception cref="NotSupportedException">The builder is not the built-in fluent rule builder.</exception>
    public static IRuleBuilder<T, TProperty> DependentRules<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        Action<ValidatorRuleBuilder<T>> configure) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        if(builder is not RuleBuilder<T, TProperty> concrete) {
            throw new NotSupportedException(
                "DependentRules requires the built-in fluent RuleBuilder<T, TProperty> implementation.");
        }

        return concrete.AddDependentRules(configure);
    }
}
