//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Assertions;

/// <summary>
/// Thrown when a validation assertion fails. The message names the failed expectation and
/// lists the actual validation errors.
/// </summary>
public sealed class ValidationAssertionException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationAssertionException"/> class.
    /// </summary>
    public ValidationAssertionException()
        : base("Validation assertion failed.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationAssertionException"/> class with a message.
    /// </summary>
    /// <param name="message">The assertion failure message.</param>
    public ValidationAssertionException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationAssertionException"/> class with a message
    /// and an inner exception.
    /// </summary>
    /// <param name="message">The assertion failure message.</param>
    /// <param name="innerException">The exception that caused this assertion failure.</param>
    public ValidationAssertionException(string message, Exception innerException)
        : base(message, innerException) { }
}
