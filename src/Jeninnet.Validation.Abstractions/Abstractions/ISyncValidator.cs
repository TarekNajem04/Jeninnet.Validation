//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Abstractions;

/// <summary>
/// Represents a synchronous validator for a specific type.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public interface ISyncValidator<in T> {
    /// <summary>
    /// Validates the specified instance.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> describing the outcome of the validation operation.
    /// </returns>
    /// <exception cref="AsyncValidatorInvokedSynchronouslyException">
    /// Thrown when this validator carries asynchronous rules but no synchronous rules;
    /// synchronous execution would silently discard the asynchronous rules.
    /// Use the <see cref="IAsyncValidator{T}"/> path or add synchronous rules.
    /// </exception>
    ValidationResult Validate(T instance);

    /// <summary>
    /// Validates the specified <paramref name="instance"/> against the configured validation rules and optional rule sets.
    /// </summary>
    /// <remarks>
    /// If specific rule sets are provided, only the rules associated with those sets are evaluated.
    /// Otherwise, all available rules are applied to the <paramref name="instance"/>.
    /// This method does not throw exceptions for validation failures; errors are reported in the returned <see cref="ValidationResult"/>.
    /// When the validator carries asynchronous rules but no synchronous rules, synchronous validation cannot run —
    /// this method throws <see cref="AsyncValidatorInvokedSynchronouslyException"/>;
    /// use <see cref="IAsyncValidator{T}.ValidateAsync(T, CancellationToken)"/> instead, or add synchronous rules.
    /// </remarks>
    /// <param name="instance">The object to validate. Represents the target of the validation operation.</param>
    /// <param name="ruleSets">
    /// An optional collection of rule set names that determine which validation rules to apply.
    /// If <see langword="null"/> or empty, all rules are applied.
    /// </param>
    /// <returns>
    /// A <see cref="ValidationResult"/> containing any validation errors found. Returns <see cref="ValidationResult.Success"/> if no errors are detected.
    /// </returns>
    /// <exception cref="AsyncValidatorInvokedSynchronouslyException">
    /// Thrown when this validator carries asynchronous rules but no synchronous rules.
    /// Use the <see cref="IAsyncValidator{T}"/> path or add synchronous rules.
    /// </exception>
    ValidationResult Validate(T instance, IEnumerable<string> ruleSets);
}
