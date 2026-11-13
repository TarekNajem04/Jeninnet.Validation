//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for configuring conditional validation rules.
/// </summary>
public static class ConditionalExtensions {
    /// <summary>
    /// Defines a group of rules that are only executed when the specified condition is satisfied.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The validator rule builder.</param>
    /// <param name="condition">The condition that must be satisfied for the rules to execute.</param>
    /// <param name="configure">
    /// A callback used to configure the rules that will be executed when the condition is satisfied.
    /// </param>
    public static void When<T>(
        this ValidatorRuleBuilder<T> builder,
        Func<T, bool> condition,
        Action<ValidatorRuleBuilder<T>> configure
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(configure);

        var (innerRules, innerAsyncRules) = BuildRules(configure);

        if(innerRules.Count is 0 && innerAsyncRules.Count is 0) {
            return;
        }

        var conditional = new ConditionalRule<T>(
            condition,
            asyncCondition: null,
            rules: innerRules,
            asyncRules: innerAsyncRules);

        builder.SyncRules.Add(conditional);
        builder.AsyncRules.Add(conditional);
    }

    /// <summary>
    /// Defines a group of rules that are only executed when the specified condition is not satisfied.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The validator rule builder.</param>
    /// <param name="condition">The condition that must be satisfied for the rules to execute.</param>
    /// <param name="configure">A callback used to configure the rules that will be executed when the condition is satisfied.</param>
    public static void Unless<T>(
        this ValidatorRuleBuilder<T> builder,
        Func<T, bool> condition,
        Action<ValidatorRuleBuilder<T>> configure
    ) => builder.When(instance => !condition(instance), configure);

    /// <summary>
    /// Defines a group of rules that are only executed when the asynchronous condition is satisfied.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The validator rule builder.</param>
    /// <param name="condition">The asynchronous condition that must be satisfied for the rules to execute.</param>
    /// <param name="configure">A callback used to configure the rules that will be executed when the condition is satisfied.</param>
    public static void WhenAsyncRule<T>(
        this ValidatorRuleBuilder<T> builder,
        Func<T, CancellationToken, Task<bool>> condition,
        Action<ValidatorRuleBuilder<T>> configure
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(configure);

        var (innerRules, innerAsyncRules) = BuildRules(configure);

        if(innerRules.Count is 0 && innerAsyncRules.Count is 0) {
            return;
        }

        var conditional = new ConditionalRule<T>(
            condition: null,
            asyncCondition: condition,
            rules: innerRules,
            asyncRules: innerAsyncRules
        );

        // Async condition means we must run through async pipeline; we also attach as async rule.
        builder.AsyncRules.Add(conditional);
    }

    /// <summary>
    /// Defines a group of rules that are only executed when the asynchronous condition is not satisfied.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The validator rule builder.</param>
    /// <param name="condition">The asynchronous condition that must be satisfied for the rules to execute.</param>
    /// <param name="configure">A callback used to configure the rules that will be executed when the condition is satisfied.</param>
    public static void UnlessAsyncRule<T>(
        this ValidatorRuleBuilder<T> builder,
        Func<T, CancellationToken, Task<bool>> condition,
        Action<ValidatorRuleBuilder<T>> configure
    ) => builder.WhenAsyncRule(
            async (instance, token) => !await condition(instance, token).ConfigureAwait(false),
            configure
        );

    private static (List<ISyncValidationRule<T>> SyncRules, List<IAsyncValidationRule<T>> AsyncRules) BuildRules<T>(
        Action<ValidatorRuleBuilder<T>> configure) {
        var syncRules = new List<ISyncValidationRule<T>>();
        var asyncRules = new List<IAsyncValidationRule<T>>();

        configure(new ValidatorRuleBuilder<T>(syncRules, asyncRules));
        return (syncRules, asyncRules);
    }
}
