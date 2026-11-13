//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Per-call execution helpers that work on any <see cref="IValidator{T}"/> implementation.
/// The fluent base validator and generated validators implement
/// <see cref="IValidationOptionsValidator{T}"/>; these helpers provide a single, explicit
/// failure mode for validators that do not.
/// </summary>
public static class ValidationOptionsExtensions {
    /// <summary>
    /// Validates an instance with per-call options.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="options">The per-call options.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> or <paramref name="options"/> is null.</exception>
    /// <exception cref="NotSupportedException">
    /// The validator does not implement <see cref="IValidationOptionsValidator{T}"/>.
    /// </exception>
    public static ValidationResult Validate<T>(
        this IValidator<T> validator,
        T instance,
        ValidationCallOptions options) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(options);

        var result = GetExecutor(validator).Validate(instance, options);
        return ThrowIfRequested(result, options);
    }

    /// <summary>
    /// Validates an instance with a chained per-call strategy.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="configureStrategy">The strategy configuration callback.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> or <paramref name="configureStrategy"/> is null.</exception>
    public static ValidationResult Validate<T>(
        this IValidator<T> validator,
        T instance,
        Action<ValidationStrategy> configureStrategy) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(configureStrategy);

        var strategy = new ValidationStrategy();
        configureStrategy(strategy);
        return Validate(validator, instance, strategy.Build());
    }

    /// <summary>
    /// Asynchronously validates an instance with per-call options.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="options">The per-call options.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> or <paramref name="options"/> is null.</exception>
    public static async ValueTask<ValidationResult> ValidateAsync<T>(
        this IValidator<T> validator,
        T instance,
        ValidationCallOptions options,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(options);

        var result = await GetExecutor(validator)
            .ValidateAsync(instance, options, cancellationToken)
            .ConfigureAwait(false);

        return ThrowIfRequested(result, options);
    }

    /// <summary>
    /// Asynchronously validates an instance with a chained per-call strategy.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="configureStrategy">The strategy configuration callback.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> or <paramref name="configureStrategy"/> is null.</exception>
    public static ValueTask<ValidationResult> ValidateAsync<T>(
        this IValidator<T> validator,
        T instance,
        Action<ValidationStrategy> configureStrategy,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(configureStrategy);

        var strategy = new ValidationStrategy();
        configureStrategy(strategy);
        return ValidateAsync(validator, instance, strategy.Build(), cancellationToken);
    }

    /// <summary>
    /// Validates an instance and throws <see cref="ValidationException"/> when validation fails.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> is null.</exception>
    public static void ValidateAndThrow<T>(this IValidator<T> validator, T instance) {
        ArgumentNullException.ThrowIfNull(validator);

        ThrowIfInvalid(validator.Validate(instance));
    }

    /// <summary>
    /// Validates an instance with the specified rule sets and throws when validation fails.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="ruleSets">The rule sets to apply.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> or <paramref name="ruleSets"/> is null.</exception>
    public static void ValidateAndThrow<T>(
        this IValidator<T> validator,
        T instance,
        IEnumerable<string> ruleSets) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(ruleSets);

        ThrowIfInvalid(validator.Validate(instance, ruleSets));
    }

    /// <summary>
    /// Validates an instance with per-call options and throws when validation fails.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="options">The per-call options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> or <paramref name="options"/> is null.</exception>
    public static void ValidateAndThrow<T>(
        this IValidator<T> validator,
        T instance,
        ValidationCallOptions options) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(options);

        ThrowIfInvalid(Validate(validator, instance, options));
    }

    /// <summary>
    /// Asynchronously validates an instance and throws when validation fails.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> is null.</exception>
    public static async ValueTask ValidateAndThrowAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(validator);

        ThrowIfInvalid(await validator.ValidateAsync(instance, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Asynchronously validates an instance with the specified rule sets and throws when validation fails.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="ruleSets">The rule sets to apply.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> or <paramref name="ruleSets"/> is null.</exception>
    public static async ValueTask ValidateAndThrowAsync<T>(
        this IValidator<T> validator,
        T instance,
        IEnumerable<string> ruleSets,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(ruleSets);

        ThrowIfInvalid(await validator.ValidateAsync(instance, ruleSets, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Asynchronously validates an instance with per-call options and throws when validation fails.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The validator.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="options">The per-call options.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> or <paramref name="options"/> is null.</exception>
    public static async ValueTask ValidateAndThrowAsync<T>(
        this IValidator<T> validator,
        T instance,
        ValidationCallOptions options,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(options);

        ThrowIfInvalid(await ValidateAsync(validator, instance, options, cancellationToken).ConfigureAwait(false));
    }

    private static IValidationOptionsValidator<T> GetExecutor<T>(IValidator<T> validator) =>
        validator as IValidationOptionsValidator<T>
        ?? throw new NotSupportedException(
            $"Validator '{validator.GetType().FullName}' does not implement " +
            $"{nameof(IValidationOptionsValidator<>)}<{typeof(T).FullName}>. " +
            "Use the basic Validate overloads or upgrade the validator.");

    private static ValidationResult ThrowIfRequested(ValidationResult result, ValidationCallOptions options) {
        if(options.ThrowOnFailures && !result.IsValid) {
            throw new ValidationException(result.Errors);
        }

        return result;
    }

    private static void ThrowIfInvalid(ValidationResult result) {
        if(!result.IsValid) {
            throw new ValidationException(result.Errors);
        }
    }
}
