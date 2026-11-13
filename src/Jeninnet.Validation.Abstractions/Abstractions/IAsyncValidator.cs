//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Abstractions;

/// <summary>
/// Represents an asynchronous validator for a specific type.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public interface IAsyncValidator<in T> {
    /// <summary>
    /// Asynchronously validates the specified instance.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the validation operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a <see cref="ValidationResult"/> describing the outcome of the validation operation.
    /// </returns>
    ValueTask<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously validates the specified <paramref name="instance"/> using the configured validation rules and optional rule sets.
    /// </summary>
    /// <param name="instance">The object to validate. Represents the target of the validation operation. Cannot be <see langword="null"/>.</param>
    /// <param name="ruleSets">
    /// An optional collection of rule set names that determine which validation rules to apply.
    /// If <see langword="null"/> or empty, all rules are applied.
    /// </param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the validation operation.</param>
    /// <returns>
    /// A task that represents the asynchronous validation operation.
    /// The task result contains a <see cref="ValidationResult"/> indicating whether the <paramref name="instance"/> is valid and any validation errors found.
    /// </returns>
    ValueTask<ValidationResult> ValidateAsync(T instance, IEnumerable<string> ruleSets, CancellationToken cancellationToken = default);
}
