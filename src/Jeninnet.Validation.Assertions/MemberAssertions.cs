//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Assertions;

/// <summary>
/// Assertions scoped to the validation errors of a single member.
/// </summary>
public sealed class MemberAssertions {
    private readonly string _memberName;
    private readonly ValidationError[] _errors;

    internal MemberAssertions(string memberName, ValidationError[] errors) {
        _memberName = memberName;
        _errors = errors;
    }

    /// <summary>
    /// Asserts the number of errors for the member.
    /// </summary>
    /// <param name="expectedCount">The expected error count.</param>
    /// <returns>The same assertions instance for chaining.</returns>
    public MemberAssertions WithCount(int expectedCount) {
        if(_errors.Length != expectedCount) {
            throw AssertionMessages.Failure(
                $"Expected {expectedCount} validation error(s) for member '{_memberName}', but found {_errors.Length}",
                _errors);
        }

        return this;
    }

    /// <summary>
    /// Asserts that at least one member error message contains the supplied fragment.
    /// </summary>
    /// <param name="messageFragment">The fragment to search for (ordinal comparison).</param>
    /// <returns>The same assertions instance for chaining.</returns>
    public MemberAssertions ContainingMessage(string messageFragment) {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageFragment);

        if(!_errors.Any(error => error.Message?.Contains(messageFragment, StringComparison.Ordinal) == true)) {
            throw AssertionMessages.Failure(
                $"Expected a validation error for member '{_memberName}' whose message contains '{messageFragment}'",
                _errors);
        }

        return this;
    }

    /// <summary>
    /// Asserts that at least one member error carries the supplied rule message key.
    /// </summary>
    /// <param name="messageKey">The expected rule message key (ordinal comparison).</param>
    /// <returns>The same assertions instance for chaining.</returns>
    public MemberAssertions ContainsRule(string messageKey) {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageKey);

        if(!_errors.Any(error => string.Equals(error.MessageKey, messageKey, StringComparison.Ordinal))) {
            throw AssertionMessages.Failure(
                $"Expected a validation error for member '{_memberName}' with rule message key '{messageKey}'",
                _errors);
        }

        return this;
    }

    /// <summary>
    /// Asserts that at least one member error carries the supplied error code.
    /// </summary>
    /// <param name="errorCode">The expected error code (ordinal comparison).</param>
    /// <returns>The same assertions instance for chaining.</returns>
    public MemberAssertions WithErrorCode(string errorCode) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        if(!_errors.Any(error => string.Equals(error.ErrorCode, errorCode, StringComparison.Ordinal))) {
            throw AssertionMessages.Failure(
                $"Expected a validation error for member '{_memberName}' with code '{errorCode}'",
                _errors);
        }

        return this;
    }

    /// <summary>
    /// Asserts that at least one member error carries the supplied severity.
    /// </summary>
    /// <param name="severity">The expected severity.</param>
    /// <returns>The same assertions instance for chaining.</returns>
    public MemberAssertions WithSeverity(ValidationErrorSeverity severity) {
        if(!_errors.Any(error => error.Severity == severity)) {
            throw AssertionMessages.Failure(
                $"Expected a validation error for member '{_memberName}' with severity '{severity}'",
                _errors);
        }

        return this;
    }

    /// <summary>
    /// Asserts that at least one member error carries the supplied custom state.
    /// </summary>
    /// <param name="expectedState">The expected custom state (compared with <see cref="object.Equals(object?, object?)"/>).</param>
    /// <returns>The same assertions instance for chaining.</returns>
    public MemberAssertions WithCustomState(object? expectedState) {
        if(!_errors.Any(error => Equals(error.CustomState, expectedState))) {
            throw AssertionMessages.Failure(
                $"Expected a validation error for member '{_memberName}' with the supplied custom state",
                _errors);
        }

        return this;
    }
}
