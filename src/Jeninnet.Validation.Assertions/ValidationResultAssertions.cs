//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Assertions;

/// <summary>
/// Fluent assertions over a <see cref="ValidationResult"/>. Every method throws
/// <see cref="ValidationAssertionException"/> when the expectation is not met.
/// </summary>
public sealed class ValidationResultAssertions {
    private readonly ValidationResult _result;

    internal ValidationResultAssertions(ValidationResult result) => _result = result;

    /// <summary>
    /// Asserts the total number of validation errors.
    /// </summary>
    /// <param name="expectedCount">The expected error count.</param>
    /// <returns>The same assertions instance for chaining.</returns>
    public ValidationResultAssertions WithErrorCount(int expectedCount) {
        if(_result.Errors.Count != expectedCount) {
            throw AssertionMessages.Failure(
                $"Expected {expectedCount} validation error(s), but found {_result.Errors.Count}",
                _result.Errors);
        }

        return this;
    }

    /// <summary>
    /// Asserts that at least one error message contains the supplied fragment.
    /// </summary>
    /// <param name="messageFragment">The fragment to search for (ordinal comparison).</param>
    /// <returns>The same assertions instance for chaining.</returns>
    public ValidationResultAssertions ContainingMessage(string messageFragment) {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageFragment);

        if(!_result.Errors.Any(error => error.Message?.Contains(messageFragment, StringComparison.Ordinal) == true)) {
            throw AssertionMessages.Failure(
                $"Expected a validation error whose message contains '{messageFragment}'",
                _result.Errors);
        }

        return this;
    }

    /// <summary>
    /// Asserts that at least one error carries the supplied error code.
    /// </summary>
    /// <param name="errorCode">The expected error code (ordinal comparison).</param>
    /// <returns>The same assertions instance for chaining.</returns>
    public ValidationResultAssertions WithErrorCode(string errorCode) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        if(!_result.Errors.Any(error => string.Equals(error.ErrorCode, errorCode, StringComparison.Ordinal))) {
            throw AssertionMessages.Failure($"Expected a validation error with code '{errorCode}'", _result.Errors);
        }

        return this;
    }

    /// <summary>
    /// Asserts that at least one error carries the supplied severity.
    /// </summary>
    /// <param name="severity">The expected severity.</param>
    /// <returns>The same assertions instance for chaining.</returns>
    public ValidationResultAssertions WithSeverity(ValidationErrorSeverity severity) {
        if(!_result.Errors.Any(error => error.Severity == severity)) {
            throw AssertionMessages.Failure($"Expected a validation error with severity '{severity}'", _result.Errors);
        }

        return this;
    }

    /// <summary>
    /// Narrows the assertions to the errors of a specific member.
    /// </summary>
    /// <param name="memberName">The member name (ordinal comparison).</param>
    /// <returns>A member-scoped assertions instance.</returns>
    public MemberAssertions ForMember(string memberName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        var errors = _result.Errors
                            .Where(error => string.Equals(error.MemberName, memberName, StringComparison.Ordinal))
                            .ToArray();

        if(errors.Length == 0) {
            throw AssertionMessages.Failure($"Expected at least one validation error for member '{memberName}'", _result.Errors);
        }

        return new MemberAssertions(memberName, errors);
    }

    /// <summary>
    /// Asserts that the result contains no validation errors.
    /// </summary>
    /// <returns>The same assertions instance for chaining.</returns>
    public ValidationResultAssertions ShouldNotHaveErrors() {
        if(!_result.IsValid) {
            throw AssertionMessages.Failure("Expected the validation result to be valid", _result.Errors);
        }

        return this;
    }
}
