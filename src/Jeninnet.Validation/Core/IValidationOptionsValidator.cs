//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Executes a validator with per-call <see cref="ValidationCallOptions"/>.
/// Implemented by the built-in composite validator, the fluent base validator, and generated
/// validators so call-site options behave the same on every authoring path.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public interface IValidationOptionsValidator<in T> {
    /// <summary>
    /// Validates the specified instance with the supplied per-call options.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="options">The per-call rule-set, cascade, fail-fast, and selection options.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(T instance, ValidationCallOptions options);

    /// <summary>
    /// Asynchronously validates the specified instance with the supplied per-call options.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="options">The per-call rule-set, cascade, fail-fast, and selection options.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    ValueTask<ValidationResult> ValidateAsync(
        T instance,
        ValidationCallOptions options,
        CancellationToken cancellationToken = default);
}
