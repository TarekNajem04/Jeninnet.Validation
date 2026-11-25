//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// A transport-neutral projection of a <see cref="ValidationError"/>. HTTP ProblemDetails and gRPC
/// status details use this projection so field, message, code, and severity stay aligned.
/// </summary>
public readonly struct ValidationErrorProjection {
    /// <summary>
    /// The field path used for errors that have no member name (model-level errors).
    /// </summary>
    public const string ModelLevelField = "$";

    /// <summary>
    /// Initializes a new projection.
    /// </summary>
    /// <param name="field">The member path; blank values become <see cref="ModelLevelField"/>.</param>
    /// <param name="message">The display message; blank values become <c>Invalid</c>.</param>
    /// <param name="errorCode">The optional programmatic error code.</param>
    /// <param name="severity">The validation severity.</param>
    public ValidationErrorProjection(
        string field,
        string message,
        string? errorCode,
        ValidationErrorSeverity severity) {
        Field = string.IsNullOrWhiteSpace(field) ? ModelLevelField : field;
        Message = string.IsNullOrWhiteSpace(message) ? "Invalid" : message;
        ErrorCode = errorCode;
        Severity = severity;
    }

    /// <summary>
    /// Gets the non-empty member path, or <see cref="ModelLevelField"/> for a model-level error.
    /// </summary>
    public string Field { get; }

    /// <summary>
    /// Gets the display message, falling back to the message key, message id, error code, or
    /// <c>Invalid</c>.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the optional programmatic error code.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the validation severity.
    /// </summary>
    public ValidationErrorSeverity Severity { get; }

    /// <summary>
    /// Projects an error using the shared field, message, code, and severity rules.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>The transport-neutral projection.</returns>
    public static ValidationErrorProjection From(ValidationError error) {
        var message = error.Message;

        if(string.IsNullOrWhiteSpace(message)) {
            message = error.MessageKey;
        }

        if(string.IsNullOrWhiteSpace(message)) {
            message = error.MessageId.Id;
        }

        if(string.IsNullOrWhiteSpace(message)) {
            message = error.ErrorCode;
        }

        return new ValidationErrorProjection(
            error.MemberName,
            message ?? "Invalid",
            error.ErrorCode,
            error.Severity);
    }
}
