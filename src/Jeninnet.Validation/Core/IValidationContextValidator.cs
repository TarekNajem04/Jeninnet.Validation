//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Executes a validator with an existing <see cref="ValidationContext{T}"/> so nested and
/// included validators preserve root, shared data, services, rule sets, fail-fast, and member path.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public interface IValidationContextValidator<T> {
    /// <summary>
    /// Validates the specified instance with the supplied context. Implementations must use the
    /// context member path when emitting errors and must not create a new validation walk.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="context">The context inherited from the parent validation walk.</param>
    /// <returns>The validation result with fully qualified member paths.</returns>
    ValidationResult Validate(T instance, ValidationContext<T> context);

    /// <summary>
    /// Asynchronously validates the specified instance with the supplied context.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="context">The context inherited from the parent validation walk.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    ValueTask<ValidationResult> ValidateAsync(
        T instance,
        ValidationContext<T> context,
        CancellationToken cancellationToken = default);
}
