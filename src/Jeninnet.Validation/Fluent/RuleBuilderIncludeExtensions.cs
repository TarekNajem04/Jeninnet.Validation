//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides fluent extension methods for including validators.
/// </summary>
public static class RuleBuilderIncludeExtensions {
    /// <summary>
    /// Includes the validation rules from another validator of the same type.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The validator to include.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static ValidatorRuleBuilder<T> Include<T>(this ValidatorRuleBuilder<T> builder, IValidator<T> validator) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(validator);

        var rule = new IncludeRule<T>(validator);
        builder.SyncRules.Add(rule);
        builder.AsyncRules.Add(rule);
        return builder;
    }

    /// <summary>
    /// Configures and includes additional validation rules inline.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="configure">An action to configure additional rules.</param>
    /// <returns>The rule builder for chaining.</returns>
    public static ValidatorRuleBuilder<T> Include<T>(this ValidatorRuleBuilder<T> builder, Action<ValidatorRuleBuilder<T>> configure) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var syncRules = new List<ISyncValidationRule<T>>();
        var asyncRules = new List<IAsyncValidationRule<T>>();
        var inner = new ValidatorRuleBuilder<T>(syncRules, asyncRules);
        configure(inner);

        if(syncRules.Count > 0 || asyncRules.Count > 0) {
            var rule = new IncludeRule<T>(new CompositeValidator<T>(syncRules, asyncRules));
            builder.SyncRules.Add(rule);
            builder.AsyncRules.Add(rule);
        }

        return builder;
    }

    /// <summary>
    /// Includes the rules of a validator declared for a base type or interface.
    /// </summary>
    /// <typeparam name="T">The derived type being validated.</typeparam>
    /// <typeparam name="TBase">The base type or interface whose rules are included.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="validator">The base validator.</param>
    /// <returns>The rule builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="validator"/> is null.
    /// </exception>
    /// <remarks>
    /// The base validator receives the derived instance as <typeparamref name="TBase"/> and inherits
    /// the parent context. Use it when the derived validator should compose, rather than replace,
    /// the base rules.
    /// </remarks>
    public static ValidatorRuleBuilder<T> IncludeBase<T, TBase>(
        this ValidatorRuleBuilder<T> builder,
        IValidator<TBase> validator)
        where T : TBase {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(validator);

        var rule = new IncludeRule<T>(new BaseValidatorAdapter<T, TBase>(validator));
        builder.SyncRules.Add(rule);
        builder.AsyncRules.Add(rule);
        return builder;
    }
}

/// <summary>
/// Adapts a validator for a base type or interface to a validator for a derived type.
/// </summary>
/// <typeparam name="T">The derived type.</typeparam>
/// <typeparam name="TBase">The base type or interface.</typeparam>
/// <param name="validator">The base validator.</param>
internal sealed class BaseValidatorAdapter<T, TBase>(IValidator<TBase> validator)
    : IValidator<T>, IValidationContextValidator<T>
    where T : TBase {
    private readonly IValidator<TBase> _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    /// <inheritdoc />
    public ValidationResult Validate(T instance) => _validator.Validate(instance);

    /// <inheritdoc />
    public ValidationResult Validate(T instance, IEnumerable<string> ruleSets) =>
        _validator.Validate(instance, ruleSets);

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default) =>
        _validator.ValidateAsync(instance, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        T instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) =>
        _validator.ValidateAsync(instance, ruleSets, cancellationToken);

    /// <inheritdoc />
    public ValidationResult Validate(T instance, ValidationContext<T> context) {
        if(_validator is IValidationContextValidator<TBase> contextValidator) {
            return contextValidator.Validate(instance, Convert(context, instance));
        }

        return _validator.Validate(instance);
    }

    /// <inheritdoc />
    public async ValueTask<ValidationResult> ValidateAsync(
        T instance,
        ValidationContext<T> context,
        CancellationToken cancellationToken = default) {
        if(_validator is IValidationContextValidator<TBase> contextValidator) {
            return await contextValidator.ValidateAsync(instance, Convert(context, instance), cancellationToken)
                                         .ConfigureAwait(false);
        }

        return await _validator.ValidateAsync(instance, cancellationToken).ConfigureAwait(false);
    }

    private static ValidationContext<TBase> Convert(ValidationContext<T> context, T instance) =>
        new(
            instance,
            context.MemberPath,
            context.RuleSets,
            context.FailFast,
            context.Root,
            context.RootData,
            context.ServiceProvider);
}
