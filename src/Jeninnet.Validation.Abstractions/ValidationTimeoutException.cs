//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Thrown when an asynchronous validation run exceeds the deadline configured by the caller.
/// </summary>
public sealed class ValidationTimeoutException : OperationCanceledException {
    /// <summary>
    /// Initializes a new exception with no budget information.
    /// </summary>
    public ValidationTimeoutException() {
    }

    /// <summary>
    /// Initializes a new exception with the supplied message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ValidationTimeoutException(string? message)
        : base(message) {
    }

    /// <summary>
    /// Initializes a new exception with the supplied message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ValidationTimeoutException(string? message, Exception? innerException)
        : base(message, innerException) {
    }

    /// <summary>
    /// Initializes a new exception for the supplied cancellation token.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    public ValidationTimeoutException(CancellationToken token)
        : base(token) {
    }

    /// <summary>
    /// Initializes a new exception for the supplied message and cancellation token.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="token">The cancellation token.</param>
    public ValidationTimeoutException(string? message, CancellationToken token)
        : base(message, token) {
    }

    /// <summary>
    /// Initializes a new exception for the supplied message, inner exception, and cancellation token.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="token">The cancellation token.</param>
    public ValidationTimeoutException(string? message, Exception? innerException, CancellationToken token)
        : base(message, innerException, token) {
    }

    /// <summary>
    /// Initializes a new exception for the supplied deadline.
    /// </summary>
    /// <param name="timeout">The configured validation budget.</param>
    public ValidationTimeoutException(TimeSpan timeout)
        : this(timeout, innerException: null) {
    }

    /// <summary>
    /// Initializes a new exception for the supplied deadline and the cancellation that observed it.
    /// </summary>
    /// <param name="timeout">The configured validation budget.</param>
    /// <param name="innerException">The cancellation exception raised by the rule, when available.</param>
    public ValidationTimeoutException(TimeSpan timeout, Exception? innerException)
        : base($"Validation exceeded the configured timeout of {timeout}.", innerException) =>
        Timeout = timeout;

    /// <summary>
    /// Gets the configured validation budget.
    /// </summary>
    public TimeSpan Timeout { get; }
}
