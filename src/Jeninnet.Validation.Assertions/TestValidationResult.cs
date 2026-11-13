//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Assertions;

/// <summary>
/// Holds the model instance and the validation result produced by <c>TestValidate</c>,
/// with assertion helpers pre-bound to both.
/// </summary>
/// <typeparam name="T">The validated model type.</typeparam>
public sealed class TestValidationResult<T> {
    internal TestValidationResult(T instance, ValidationResult result) {
        Instance = instance;
        Result = result;
    }

    /// <summary>
    /// Gets the validated model instance.
    /// </summary>
    public T Instance { get; }

    /// <summary>
    /// Gets the raw validation result.
    /// </summary>
    public ValidationResult Result { get; }

    /// <summary>
    /// Asserts that the result contains validation errors.
    /// </summary>
    /// <returns>Result-scoped assertions for chaining.</returns>
    public ValidationResultAssertions ShouldHaveValidationErrors() => Result.ShouldHaveValidationErrors();

    /// <summary>
    /// Asserts that the result contains no validation errors.
    /// </summary>
    /// <returns>The same test result for chaining.</returns>
    public TestValidationResult<T> ShouldNotHaveValidationErrors() {
        _ = Result.ShouldNotHaveValidationErrors();
        return this;
    }

    /// <summary>
    /// Asserts that the member has at least one validation error and narrows the assertions to it.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <returns>Member-scoped assertions for chaining.</returns>
    public MemberAssertions ShouldHaveValidationErrorFor(string memberName) =>
        Result.ShouldHaveValidationErrors().ForMember(memberName);

    /// <summary>
    /// Asserts that the member has no validation errors (other members may still have errors).
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <returns>The same test result for chaining.</returns>
    public TestValidationResult<T> ShouldNotHaveValidationErrorFor(string memberName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        var errors = Result.Errors
                           .Where(error => string.Equals(error.MemberName, memberName, StringComparison.Ordinal))
                           .ToArray();

        if(errors.Length > 0) {
            throw AssertionMessages.Failure(
                $"Expected no validation errors for member '{memberName}', but found {errors.Length}",
                errors);
        }

        return this;
    }
}
