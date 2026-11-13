//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for defining named rule sets.
/// </summary>
public static class RuleSetExtensions {
    /// <summary>
    /// Places the rule immediately before this call in a named rule set. This is the per-rule form;
    /// use <see cref="RuleSet{T}(ValidatorRuleBuilder{T}, string, Action{ValidatorRuleBuilder{T}})"/>
    /// to group several rules.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The member type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="name">The rule-set name.</param>
    /// <returns>The same rule builder for further chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// The builder is not a <see cref="RuleBuilder{T, TProperty}"/> or no rule precedes the call.
    /// </exception>
    public static IRuleBuilder<T, TProperty> RuleSet<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        string name) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if(builder is not RuleBuilder<T, TProperty> ruleBuilder) {
            throw new InvalidOperationException(
                "RuleSet must directly follow a rule created by RuleFor.");
        }

        ruleBuilder.ApplyRuleSetToLastRule(name);
        return builder;
    }

    /// <summary>
    /// Defines a named rule set containing the rules configured in the provided callback.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The validator rule builder.</param>
    /// <param name="name">The name of the rule set.</param>
    /// <param name="configure">A callback used to configure the rules in the rule set.</param>
    public static void RuleSet<T>(this ValidatorRuleBuilder<T> builder, string name, Action<ValidatorRuleBuilder<T>> configure) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(configure);

        var innerRules = new List<ISyncValidationRule<T>>();
        var innerAsyncRules = new List<IAsyncValidationRule<T>>();

        var innerBuilder = new ValidatorRuleBuilder<T>(innerRules, innerAsyncRules);
        configure(innerBuilder);

        if(innerRules.Count is 0 && innerAsyncRules.Count is 0) {
            return;
        }

        var ruleSet = new RuleSetRule<T>([name], innerRules, innerAsyncRules);

        if(RuleContentInspector.HasSyncContent(innerRules)) {
            builder.SyncRules.Add(ruleSet);
        }

        if(innerAsyncRules.Count > 0) {
            builder.AsyncRules.Add(ruleSet);
        }
    }
}
