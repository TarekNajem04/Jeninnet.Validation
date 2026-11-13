//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Represents an asynchronous validation rule for a specific type.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public interface IAsyncValidationRule<T> {
    /// <summary>
    /// Asynchronously executes the validation rule.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">
    /// The collection to which any generated <see cref="ValidationError"/> instances should be added.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for completion.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask ValidateAsync(
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken = default
    );
}
