//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for configuring nested validation rules.
/// </summary>
public static class RuleBuilderNestedExtensions {
    // =======================================================================
    //  Nested
    // =======================================================================
    /// <summary>
    /// Applies a child validator to the property.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="syncValidator">The synchronous validator for the property.</param>
    /// <param name="asyncValidator">The asynchronous validator for the property.</param>
    public static IRuleBuilder<T, TProperty?> SetValidator<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder,
        ISyncValidator<TProperty>? syncValidator,
        IAsyncValidator<TProperty>? asyncValidator
    ) {
        ArgumentNullException.ThrowIfNull(builder);

        if(syncValidator is null && asyncValidator is null) {
            return builder;
        }

        var rule = new NestedValidatorRule<T, TProperty>(builder.Accessor, syncValidator, asyncValidator);

        // The rule is always registered as sync content so an async-only nested validator
        // is detected by the strict-async guard instead of being silently ignored.
        builder.AddRule(rule);

        if(asyncValidator is not null) {
            builder.AddAsyncRule(rule);
        }

        return builder;
    }

    /// <summary>
    /// Applies a child validator to the property.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The validator for the property.</param>
    public static IRuleBuilder<T, TProperty?> SetValidator<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder,
        IValidator<TProperty> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.SetValidator(validator, validator);
    }

    /// <summary>
    /// Applies a child validator to the property.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The synchronous validator for the property.</param>
    public static IRuleBuilder<T, TProperty?> SetValidator<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder,
        ISyncValidator<TProperty> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.SetValidator(validator, null);
    }

    /// <summary>
    /// Applies an asynchronous child validator to the property.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The asynchronous validator for the property.</param>
    public static IRuleBuilder<T, TProperty?> SetValidator<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder,
        IAsyncValidator<TProperty> validator
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.SetValidator(null, validator);
    }

    // -----------------------------------------------------------------------
    // CHILD RULES
    // -----------------------------------------------------------------------

    /// <summary>
    /// Configures inline child validation rules for a nested property.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The nested property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">An action to configure rules on the nested property.</param>
    public static IRuleBuilder<T, TProperty?> ChildRules<T, TProperty>(
        this IRuleBuilder<T, TProperty?> builder,
        Action<ValidatorRuleBuilder<TProperty>> configure
    ) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var syncRules = new List<ISyncValidationRule<TProperty>>();
        var asyncRules = new List<IAsyncValidationRule<TProperty>>();
        var inner = new ValidatorRuleBuilder<TProperty>(syncRules, asyncRules);
        configure(inner);

        var rule = new ChildRulesRule<T, TProperty>(builder.Accessor, syncRules, asyncRules);

        if(RuleContentInspector.HasSyncContent(syncRules)) {
            builder.AddRule(rule);
        }

        if(asyncRules.Count > 0) {
            builder.AddAsyncRule(rule);
        }

        return builder;
    }
}
