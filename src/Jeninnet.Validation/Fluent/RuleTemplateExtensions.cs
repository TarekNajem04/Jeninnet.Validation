//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Adds reusable <see cref="RuleTemplate{TValue}"/> rules to a fluent member builder.
/// </summary>
public static class RuleTemplateExtensions {
    /// <summary>
    /// Adds a value-only rule template. The template predicate receives the member value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The member value type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="template">The reusable template.</param>
    /// <returns>The same rule builder for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="template"/> is null.</exception>
    public static IRuleBuilder<T, TValue> Rule<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        RuleTemplate<TValue> template) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(template);

        builder.AddRule(new RuleTemplateRule<T, TValue>(builder.Accessor, template));
        return builder;
    }

    /// <summary>
    /// Adds an instance-aware rule template. The template predicate receives the model instance and
    /// the member value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TValue">The member value type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="template">The reusable template.</param>
    /// <returns>The same rule builder for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="template"/> is null.</exception>
    public static IRuleBuilder<T, TValue> Rule<T, TValue>(
        this IRuleBuilder<T, TValue> builder,
        RuleTemplate<T, TValue> template) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(template);

        builder.AddRule(new RuleTemplateRule<T, TValue>(builder.Accessor, template));
        return builder;
    }
}
