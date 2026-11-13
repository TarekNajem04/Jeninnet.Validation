//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides extension methods for configuring validation rules after they have been defined.
/// </summary>
public static class RuleBuilderOptionsExtensions {
    /// <summary>
    /// Sets a custom error message for the last added rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">The custom error message.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilder<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilder<T, TProperty> builder, string message) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(message);

        ConfigureLastRule(builder, options => {
            options.Message = message;
            options.HasMessage = true;
        });

        return builder;
    }

    /// <summary>
    /// Sets a dynamic custom error message for the last added rule. The delegate runs only when
    /// the rule fails and receives the instance being validated.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">The message factory.</param>
    /// <returns>The rule builder for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="message"/> is null.</exception>
    public static IRuleBuilder<T, TProperty> WithMessage<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        Func<T, string> message) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(message);

        ConfigureLastRule(builder, options => options.MessageFactory = message);
        return builder;
    }

    /// <summary>
    /// Sets a dynamic custom error message that also receives the validation context.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="message">The message factory.</param>
    /// <returns>The rule builder for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="message"/> is null.</exception>
    public static IRuleBuilder<T, TProperty> WithMessage<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        Func<T, ValidationContext<T>, string> message) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(message);

        ConfigureLastRule(builder, options => options.MessageFactory = message);
        return builder;
    }

    /// <summary>
    /// Sets a custom error code for the last added rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="errorCode">The custom error code.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilder<T, TProperty> WithCode<T, TProperty>(this IRuleBuilder<T, TProperty> builder, string errorCode) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(errorCode);

        ConfigureLastRule(builder, options => {
            options.ErrorCode = errorCode;
            options.HasErrorCode = true;
        });

        return builder;
    }

    /// <summary>
    /// Sets a custom severity level for the last added rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="severity">The severity level.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilder<T, TProperty> WithSeverity<T, TProperty>(this IRuleBuilder<T, TProperty> builder, ValidationErrorSeverity severity) {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureLastRule(builder, options => {
            options.Severity = severity;
            options.HasSeverity = true;
        });

        return builder;
    }

    /// <summary>
    /// Overrides the property name reported in validation errors for the last added rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="name">The custom property name.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilder<T, TProperty> OverridePropertyName<T, TProperty>(this IRuleBuilder<T, TProperty> builder, string name) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        ConfigureLastRule(builder, options => {
            options.MemberName = name;
            options.HasMemberName = true;
        });

        return builder;
    }

    /// <summary>
    /// Sets a custom member name for the last added rule using an instance-aware factory.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="name">The member-name factory.</param>
    /// <returns>The rule builder for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="name"/> is null.</exception>
    public static IRuleBuilder<T, TProperty> OverridePropertyName<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        Func<T, string> name) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        ConfigureLastRule(builder, options => options.MemberNameFactory = name);
        return builder;
    }

    /// <summary>
    /// Sets a custom property name for the last added rule (alias for the string overload of
    /// <c>OverridePropertyName</c>).
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="name">The custom property name.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilder<T, TProperty> WithName<T, TProperty>(this IRuleBuilder<T, TProperty> builder, string name) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return OverridePropertyName(builder, name);
    }

    /// <summary>
    /// Sets a dynamic display name for the last added rule. The display name affects the
    /// <c>{PropertyName}</c> message parameter and leaves <see cref="ValidationError.MemberName"/>
    /// unchanged.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="name">The display-name factory.</param>
    /// <returns>The rule builder for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="name"/> is null.</exception>
    public static IRuleBuilder<T, TProperty> WithName<T, TProperty>(
        this IRuleBuilder<T, TProperty> builder,
        Func<T, string> name) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        ConfigureLastRule(builder, options => options.DisplayNameFactory = name);
        return builder;
    }

    /// <summary>
    /// Attaches a custom state object to the last added rule.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="state">The custom state object.</param>
    /// <returns>The rule builder for further chaining.</returns>
    public static IRuleBuilder<T, TProperty> WithState<T, TProperty>(this IRuleBuilder<T, TProperty> builder, object? state) {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureLastRule(builder, options => {
            options.CustomState = state;
            options.HasCustomState = true;
        });

        return builder;
    }

    /// <summary>
    /// Attaches a context-aware custom state factory to the last added rule. The factory runs when
    /// an error is emitted and receives the model, the member value, and the validation context.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="stateFactory">The custom-state factory.</param>
    /// <returns>The rule builder for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="stateFactory"/> is null.</exception>
    public static IRuleBuilder<T, TProperty> WithState<T, TProperty, TState>(
        this IRuleBuilder<T, TProperty> builder,
        Func<T, TProperty, ValidationContext<T>, TState> stateFactory) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(stateFactory);

        ConfigureLastRule(builder, options => options.CustomStateFactory =
            new CustomStateFactory<T, TProperty>((model, value, context) => stateFactory(model, value, context)));

        return builder;
    }

    /// <summary>
    /// Attaches a context-aware custom state factory to the last added rule. This overload omits the
    /// validation context.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="builder">The rule builder.</param>
    /// <param name="stateFactory">The custom-state factory.</param>
    /// <returns>The rule builder for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="stateFactory"/> is null.</exception>
    public static IRuleBuilder<T, TProperty> WithState<T, TProperty, TState>(
        this IRuleBuilder<T, TProperty> builder,
        Func<T, TProperty, TState> stateFactory) {
        ArgumentNullException.ThrowIfNull(stateFactory);

        return WithState<T, TProperty, TState>(
            builder,
            (instance, value, _) => stateFactory(instance, value));
    }

    private static void ConfigureLastRule<T, TProperty>(
        IRuleBuilder<T, TProperty> builder,
        Action<RuleOptionOverrides> configure) {
        if(builder is not IRuleOptionsTargetBuilder ruleBuilder) {
            throw new NotSupportedException(
                "Rule options require a fluent rule builder that supports per-rule overrides.");
        }

        var options = new RuleOptionOverrides();
        configure(options);
        ruleBuilder.ConfigureLastRule(options);
    }
}
