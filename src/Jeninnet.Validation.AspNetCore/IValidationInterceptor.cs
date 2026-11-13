//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Optional hook around request validation in MVC and minimal APIs. Interceptors run in
/// registration order; no interceptor means no added branches.
/// </summary>
public interface IValidationInterceptor {
    /// <summary>
    /// Runs before the validator. Return a non-null result to short-circuit validation and use it
    /// as the final result. The result still passes through
    /// <see cref="AfterValidationAsync"/>.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A replacement result, or <see langword="null"/> to continue validation.</returns>
    ValueTask<ValidationResult?> BeforeValidationAsync(
        ValidationInterceptorContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs after validation. Return the original result or a replacement result.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="result">The current validation result.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The result to use for the response.</returns>
    ValueTask<ValidationResult> AfterValidationAsync(
        ValidationInterceptorContext context,
        ValidationResult result,
        CancellationToken cancellationToken = default);
}
