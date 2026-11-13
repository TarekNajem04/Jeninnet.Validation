//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR;

/// <summary>
/// Provides consistent mappings from <see cref="ValidationError"/> values to result-library
/// codes, messages, and metadata.
/// </summary>
public static class ValidationErrorFormatter {
    private const string FALLBACK_CODE = "Validation";
    private const string FALLBACK_MESSAGE = "Invalid";
    private const string SEPARATOR = "; ";

    /// <summary>
    /// Gets the programmatic code for an error. The error code wins, then the message key,
    /// then the message identifier.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>The error code.</returns>
    public static string GetCode(ValidationError error) {
        if(!string.IsNullOrWhiteSpace(error.ErrorCode)) {
            return error.ErrorCode;
        }

        if(!string.IsNullOrWhiteSpace(error.MessageKey)) {
            return error.MessageKey;
        }

        if(!string.IsNullOrWhiteSpace(error.MessageId.Id)) {
            return error.MessageId.Id;
        }

        return FALLBACK_CODE;
    }

    /// <summary>
    /// Gets the display message for an error. The custom message wins, then the message key,
    /// then the message identifier.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>The display message.</returns>
    public static string GetMessage(ValidationError error) {
        if(!string.IsNullOrWhiteSpace(error.Message)) {
            return error.Message;
        }

        if(!string.IsNullOrWhiteSpace(error.MessageKey)) {
            return error.MessageKey;
        }

        if(!string.IsNullOrWhiteSpace(error.MessageId.Id)) {
            return error.MessageId.Id;
        }

        return FALLBACK_MESSAGE;
    }

    /// <summary>
    /// Gets the structured metadata for an error: member name, severity, message key, error code,
    /// message identifier, message parameters, and custom state.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>A new metadata dictionary.</returns>
    public static Dictionary<string, object> GetMetadata(ValidationError error) {
        var metadata = new Dictionary<string, object>(StringComparer.Ordinal) {
            ["severity"] = error.Severity.ToString()
        };

        if(!string.IsNullOrWhiteSpace(error.MemberName)) {
            metadata["memberName"] = error.MemberName;
        }

        if(!string.IsNullOrWhiteSpace(error.MessageKey)) {
            metadata["messageKey"] = error.MessageKey;
        }

        if(!string.IsNullOrWhiteSpace(error.ErrorCode)) {
            metadata["errorCode"] = error.ErrorCode;
        }

        if(!string.IsNullOrWhiteSpace(error.MessageId.Id)) {
            metadata["messageId"] = error.MessageId.Id;
        }

        if(error.MessageParameters is { Count: > 0 }) {
            metadata["messageParameters"] = error.MessageParameters;
        }

        if(error.CustomState is not null) {
            metadata["customState"] = error.CustomState;
        }

        return metadata;
    }

    /// <summary>
    /// Formats one error as text, including severity, member name, and code when available.
    /// Use this for result libraries that only accept a string.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>The formatted message.</returns>
    public static string Format(ValidationError error) {
        var message = GetMessage(error);

        if(error.Severity is not ValidationErrorSeverity.Error) {
            message = $"[{error.Severity}] {message}";
        }

        if(!string.IsNullOrWhiteSpace(error.MemberName)) {
            message = $"{error.MemberName}: {message}";
        }

        var code = GetCode(error);

        return string.Equals(code, FALLBACK_CODE, StringComparison.Ordinal)
            ? message
            : $"{message} ({code})";
    }

    /// <summary>
    /// Joins every error into one text message, preserving severity, member names, and codes.
    /// </summary>
    /// <param name="result">The failed validation result.</param>
    /// <param name="separator">The separator between errors. Defaults to <c>"; "</c>.</param>
    /// <returns>The joined message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="separator"/> is null.</exception>
    public static string JoinMessages(ValidationResult result, string separator = SEPARATOR) {
        ArgumentNullException.ThrowIfNull(separator);

        return string.Join(separator, result.Errors.Select(Format));
    }
}
