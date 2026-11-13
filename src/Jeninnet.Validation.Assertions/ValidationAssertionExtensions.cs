//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Assertions;

/// <summary>
/// Entry points for the stack-agnostic validation assertions.
/// </summary>
public static class ValidationAssertionExtensions {
    /// <summary>
    /// Asserts that the result contains at least one validation error.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <returns>Result-scoped assertions for chaining.</returns>
    /// <exception cref="ValidationAssertionException">The result is valid.</exception>
    public static ValidationResultAssertions ShouldHaveValidationErrors(this ValidationResult result) {
        if(result.IsValid) {
            throw AssertionMessages.Failure("Expected validation errors, but the result is valid", result.Errors);
        }

        return new ValidationResultAssertions(result);
    }

    /// <summary>
    /// Asserts that the result contains no validation errors.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <returns>Result-scoped assertions for chaining.</returns>
    /// <exception cref="ValidationAssertionException">The result contains errors.</exception>
    public static ValidationResultAssertions ShouldNotHaveValidationErrors(this ValidationResult result) =>
        new ValidationResultAssertions(result).ShouldNotHaveErrors();

    /// <summary>
    /// Validates the instance and returns assertions bound to the model and the result.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="validator">The synchronous validator.</param>
    /// <param name="instance">The model instance.</param>
    /// <returns>A test result that exposes the raw result and assertion helpers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> or <paramref name="instance"/> is null.</exception>
    public static TestValidationResult<T> TestValidate<T>(this ISyncValidator<T> validator, T instance) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(instance);

        return new TestValidationResult<T>(instance, validator.Validate(instance));
    }

    /// <summary>
    /// Validates the instance asynchronously and returns assertions bound to the model and the result.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="validator">The asynchronous validator.</param>
    /// <param name="instance">The model instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A test result that exposes the raw result and assertion helpers.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validator"/> or <paramref name="instance"/> is null.</exception>
    public static async ValueTask<TestValidationResult<T>> TestValidateAsync<T>(
        this IAsyncValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(instance);

        var result = await validator.ValidateAsync(instance, cancellationToken).ConfigureAwait(false);
        return new TestValidationResult<T>(instance, result);
    }
}
