//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides a base class for defining validators using a fluent API.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public abstract class Validator<T> : ValidatorDefinition<T>, IValidator<T>, IValidationOptionsValidator<T>, IValidationContextValidator<T> {

    /// <summary>
    /// Initializes a new instance of the <see cref="Validator{T}"/> class.
    /// </summary>
    /// <param name="ruleLevelCascadeMode">
    /// An explicit rule-level cascade mode for this validator, or <see langword="null"/> to use
    /// the effective <see cref="ValidationOptions.DefaultRuleLevelCascadeMode"/> at validation time.
    /// </param>
    protected Validator(CascadeMode? ruleLevelCascadeMode = null)
        : base(ruleLevelCascadeMode) =>
        InnerValidator = Build();

    /// <summary>
    /// Gets the composite validator used to perform validation operations on the current object.
    /// </summary>
    protected CompositeValidator<T> InnerValidator { get; }

    /// <inheritdoc/>
    public ValidationResult Validate(T instance) =>
        ValidateCore(instance, ruleSets: null, ExplicitClassLevelCascadeMode, failFast: false, filter: default);

    /// <inheritdoc/>
    public ValidationResult Validate(T instance, IEnumerable<string> ruleSets) =>
        ValidateCore(instance, ruleSets, ExplicitClassLevelCascadeMode, failFast: false, filter: default);

    /// <summary>
    /// Validates the specified instance with per-call options (rule sets, class-level cascade
    /// override, and fail-fast).
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="options">The per-call options.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public ValidationResult Validate(T instance, ValidationCallOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        var result = ValidateCore(
            instance,
            options.RuleSets,
            options.CascadeMode ?? ExplicitClassLevelCascadeMode,
            options.FailFast,
            new ValidationFilter(
                options.IncludeProperties,
                options.ExcludeProperties,
                options.ExcludeRuleSets,
                options.ServiceProvider,
                options.IncludeAllRuleSets,
                options.Selector));

        return ThrowIfRequested(result, options);
    }

    /// <summary>
    /// Validates the specified instance using a chained per-call strategy.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="configureStrategy">The strategy configuration callback.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configureStrategy"/> is null.</exception>
    public ValidationResult Validate(T instance, Action<ValidationStrategy> configureStrategy) {
        ArgumentNullException.ThrowIfNull(configureStrategy);

        var strategy = new ValidationStrategy();
        configureStrategy(strategy);
        return Validate(instance, strategy.Build());
    }

    /// <inheritdoc/>
    public ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default) =>
        ValidateCoreAsync(instance, ruleSets: null, ExplicitClassLevelCascadeMode, failFast: false, filter: default, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<ValidationResult> ValidateAsync(T instance, IEnumerable<string> ruleSets, CancellationToken cancellationToken = default) =>
        ValidateCoreAsync(instance, ruleSets, ExplicitClassLevelCascadeMode, failFast: false, filter: default, cancellationToken);

    /// <summary>
    /// Validates the specified instance asynchronously with per-call options (rule sets,
    /// class-level cascade override, and fail-fast).
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="options">The per-call options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public async ValueTask<ValidationResult> ValidateAsync(T instance, ValidationCallOptions options, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(options);

        var result = await ValidateCoreAsync(
            instance,
            options.RuleSets,
            options.CascadeMode ?? ExplicitClassLevelCascadeMode,
            options.FailFast,
            new ValidationFilter(
                options.IncludeProperties,
                options.ExcludeProperties,
                options.ExcludeRuleSets,
                options.ServiceProvider,
                options.IncludeAllRuleSets,
                options.Selector),
            cancellationToken).ConfigureAwait(false);

        return ThrowIfRequested(result, options);
    }

    /// <summary>
    /// Validates the specified instance asynchronously using a chained per-call strategy.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="configureStrategy">The strategy configuration callback.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configureStrategy"/> is null.</exception>
    public ValueTask<ValidationResult> ValidateAsync(
        T instance,
        Action<ValidationStrategy> configureStrategy,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(configureStrategy);

        var strategy = new ValidationStrategy();
        configureStrategy(strategy);
        return ValidateAsync(instance, strategy.Build(), cancellationToken);
    }

    /// <inheritdoc />
    public ValidationResult Validate(T instance, ValidationContext<T> context) {
        if(PreValidateAsyncHook is not null) {
            throw new InvalidOperationException(
                "An asynchronous PreValidate hook is configured on this validator; call ValidateAsync instead.");
        }

        if(PreValidateHook is not null && !InvokePreValidate(PreValidateHook, context)) {
            return ValidationResult.Success;
        }

        var result = InnerValidator.Validate(instance, context, ExplicitClassLevelCascadeMode);

        if(!result.IsValid && OnFailureHook is not null) {
            InvokeOnFailure(OnFailureHook, result);
        }

        return result;
    }

    /// <inheritdoc />
    public async ValueTask<ValidationResult> ValidateAsync(
        T instance,
        ValidationContext<T> context,
        CancellationToken cancellationToken = default) {
        if(PreValidateHook is not null && !InvokePreValidate(PreValidateHook, context)) {
            return ValidationResult.Success;
        }

        if(PreValidateAsyncHook is not null &&
           !await InvokePreValidateAsync(PreValidateAsyncHook, context, cancellationToken).ConfigureAwait(false)) {
            return ValidationResult.Success;
        }

        var result = await InnerValidator.ValidateAsync(instance, context, ExplicitClassLevelCascadeMode, cancellationToken).ConfigureAwait(false);

        if(!result.IsValid) {
            if(OnFailureHook is not null) {
                InvokeOnFailure(OnFailureHook, result);
            }

            if(OnFailureAsyncHook is not null) {
                await InvokeOnFailureAsync(OnFailureAsyncHook, result, cancellationToken).ConfigureAwait(false);
            }
        }

        return result;
    }

    private ValidationResult ValidateCore(T instance, IEnumerable<string>? ruleSets, CascadeMode? classLevelCascadeMode, bool failFast, ValidationFilter filter) {
        if(PreValidateAsyncHook is not null) {
            throw new InvalidOperationException(
                "An asynchronous PreValidate hook is configured on this validator; call ValidateAsync instead.");
        }

        var context = new ValidationContext<T>(
            instance,
            MemberPath: string.Empty,
            ruleSets,
            failFast,
            Options: ValidationConfiguration.ResolveOptions(filter.ServiceProvider));

        if(PreValidateHook is not null && !InvokePreValidate(PreValidateHook, context)) {
            return ValidationResult.Success;
        }

        var result = InnerValidator.Validate(instance, ruleSets, classLevelCascadeMode, failFast, filter);

        if(!result.IsValid && OnFailureHook is not null) {
            InvokeOnFailure(OnFailureHook, result);
        }

        return result;
    }

    private async ValueTask<ValidationResult> ValidateCoreAsync(
        T instance,
        IEnumerable<string>? ruleSets,
        CascadeMode? classLevelCascadeMode,
        bool failFast,
        ValidationFilter filter,
        CancellationToken cancellationToken) {
        var context = new ValidationContext<T>(
            instance,
            MemberPath: string.Empty,
            ruleSets,
            failFast,
            Options: ValidationConfiguration.ResolveOptions(filter.ServiceProvider));

        if(PreValidateHook is not null && !InvokePreValidate(PreValidateHook, context)) {
            return ValidationResult.Success;
        }

        if(PreValidateAsyncHook is not null &&
           !await InvokePreValidateAsync(PreValidateAsyncHook, context, cancellationToken).ConfigureAwait(false)) {
            return ValidationResult.Success;
        }

        var result = await InnerValidator.ValidateAsync(instance, ruleSets, classLevelCascadeMode, failFast, filter, cancellationToken).ConfigureAwait(false);

        if(!result.IsValid) {
            if(OnFailureHook is not null) {
                InvokeOnFailure(OnFailureHook, result);
            }

            if(OnFailureAsyncHook is not null) {
                await InvokeOnFailureAsync(OnFailureAsyncHook, result, cancellationToken).ConfigureAwait(false);
            }
        }

        return result;
    }

    private static ValidationResult ThrowIfRequested(ValidationResult result, ValidationCallOptions options) {
        if(options.ThrowOnFailures && !result.IsValid) {
            throw new ValidationException(result.Errors);
        }

        return result;
    }

    private static bool InvokePreValidate(Func<ValidationContext<T>, bool> hook, ValidationContext<T> context) {
        try {
            return hook(context);
        }
        catch(Exception exception) when(exception is not OperationCanceledException) {
            throw new ValidationException("The PreValidate hook threw an exception.", exception);
        }
    }

    private static async ValueTask<bool> InvokePreValidateAsync(
        Func<ValidationContext<T>, CancellationToken, ValueTask<bool>> hook,
        ValidationContext<T> context,
        CancellationToken cancellationToken) {
        try {
            return await hook(context, cancellationToken).ConfigureAwait(false);
        }
        catch(Exception exception) when(exception is not OperationCanceledException) {
            throw new ValidationException("The PreValidateAsync hook threw an exception.", exception);
        }
    }

    private static void InvokeOnFailure(Action<ValidationResult> hook, ValidationResult result) {
        try {
            hook(result);
        }
        catch(Exception exception) when(exception is not OperationCanceledException) {
            throw new ValidationException("The OnFailure hook threw an exception.", exception);
        }
    }

    private static async ValueTask InvokeOnFailureAsync(
        Func<ValidationResult, CancellationToken, ValueTask> hook,
        ValidationResult result,
        CancellationToken cancellationToken) {
        try {
            await hook(result, cancellationToken).ConfigureAwait(false);
        }
        catch(Exception exception) when(exception is not OperationCanceledException) {
            throw new ValidationException("The OnFailureAsync hook threw an exception.", exception);
        }
    }

    /// <summary>
    /// Validates the specified instance and throws a <see cref="ValidationException"/> if validation fails.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <exception cref="ValidationException">Thrown when the validation result contains errors.</exception>
    public void ValidateAndThrow(T instance) {
        var result = Validate(instance);
        if(!result.IsValid) {
            throw new ValidationException(result.Errors);
        }
    }

    /// <summary>
    /// Validates the specified instance using the specified rule sets and throws a <see cref="ValidationException"/> if validation fails.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="ruleSets">The rule sets to apply.</param>
    /// <exception cref="ValidationException">Thrown when the validation result contains errors.</exception>
    public void ValidateAndThrow(T instance, IEnumerable<string> ruleSets) {
        var result = Validate(instance, ruleSets);
        if(!result.IsValid) {
            throw new ValidationException(result.Errors);
        }
    }

    /// <summary>
    /// Asynchronously validates the specified instance and throws a <see cref="ValidationException"/> if validation fails.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ValidationException">Thrown when the validation result contains errors.</exception>
    public async ValueTask ValidateAndThrowAsync(T instance, CancellationToken cancellationToken = default) {
        var result = await ValidateAsync(instance, cancellationToken).ConfigureAwait(false);
        if(!result.IsValid) {
            throw new ValidationException(result.Errors);
        }
    }

    /// <summary>
    /// Asynchronously validates the specified instance using the specified rule sets and throws a <see cref="ValidationException"/> if validation fails.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="ruleSets">The rule sets to apply.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ValidationException">Thrown when the validation result contains errors.</exception>
    public async ValueTask ValidateAndThrowAsync(T instance, IEnumerable<string> ruleSets, CancellationToken cancellationToken = default) {
        var result = await ValidateAsync(instance, ruleSets, cancellationToken).ConfigureAwait(false);
        if(!result.IsValid) {
            throw new ValidationException(result.Errors);
        }
    }
}
