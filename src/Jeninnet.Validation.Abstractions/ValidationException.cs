//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Represents errors that occur when validation fails.
/// </summary>
public class ValidationException : Exception {
    /// <summary>
    /// Gets the collection of validation errors that caused the exception.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    public ValidationException()
        : base("Validation failed.") => Errors = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a list of validation errors.
    /// </summary>
    /// <param name="errors">The validation errors that caused the exception.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is <see langword="null"/>.</exception>
    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base($"Validation failed. {errors?.Count} error(s) found.") {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ValidationException(string message)
        : base(message) => Errors = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ValidationException(string message, Exception innerException)
        : base(message, innerException) => Errors = [];
}
