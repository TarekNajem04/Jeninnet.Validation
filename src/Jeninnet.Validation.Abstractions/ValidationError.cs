//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Represents a single validation error associated with a specific member.
/// </summary>
public readonly record struct ValidationError {
    /// <summary>
    /// Gets the name of the member (e.g., property) that the validation error is associated with.
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// Gets the message identifier that corresponds to the validation error.
    /// This can be used to look up localized error messages or to categorize errors.
    /// </summary>
    public MessageId MessageId { get; }

    /// <summary>
    /// Gets the optional message key that can be used to look up a localized message.
    /// </summary>
    public string? MessageKey { get; }

    /// <summary>
    /// Gets the parameters used to format the validation message.
    /// This allows for dynamic insertion of values into error messages based on the context of the validation failure.
    /// </summary>
    public IDictionary<string, object>? MessageParameters { get; }

    /// <summary>
    /// Gets the optional custom message that can override the default message associated with the MessageId.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the optional error code that can be used for programmatic handling of specific validation errors.
    /// This can be useful for client applications to identify and respond to particular error conditions without relying on message text.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the severity level of the validation error, which can be used to differentiate between errors, warnings, and informational messages.
    /// </summary>
    public ValidationErrorSeverity Severity { get; }

    /// <summary>
    /// Gets an optional custom state object associated with the validation error.
    /// </summary>
    public object? CustomState { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Message"/> was authored explicitly
    /// (for example with <c>WithMessage</c>, an attribute message, or a direct custom message).
    /// Authored messages take precedence over localization packs.
    /// </summary>
    public bool MessageIsAuthored { get; init; }

    /// <summary>
    /// Initializes a new validation error with a message identifier and optional formatting parameters.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="messageId">The identifier for the validation message.</param>
    /// <param name="messageKey">Optional key for looking up a localized message.</param>
    /// <param name="messageParameters">Optional parameters for formatting the message.</param>
    /// <param name="message">Optional override message text.</param>
    /// <param name="errorCode">Optional programmatic error code.</param>
    /// <param name="severity">The severity of the validation error.</param>
    /// <param name="customState">Optional custom state object.</param>
#pragma warning disable S107 // Constructor parameter count exceeds threshold — all but two parameters have defaults; this is a data record with many optional fields
    public ValidationError(
#pragma warning restore S107
        string memberName,
        MessageId messageId,
        string? messageKey = null,
        IDictionary<string, object>? messageParameters = null,
        string? message = null,
        string? errorCode = null,
        ValidationErrorSeverity severity = ValidationErrorSeverity.Error,
        object? customState = null
    ) {
        MemberName = memberName;
        MessageId = messageId;
        MessageKey = messageKey;
        MessageParameters = messageParameters;
        Message = message;
        ErrorCode = errorCode;
        Severity = severity;
        CustomState = customState;
    }

    /// <summary>
    /// Initializes a new validation error with a direct message and no message identifier.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="message">The validation error message.</param>
    /// <param name="errorCode">Optional programmatic error code.</param>
    /// <param name="messageParameters">Optional parameters for formatting the message.</param>
    /// <param name="severity">The severity of the validation error.</param>
    /// <param name="customState">Optional custom state object.</param>
    public ValidationError(
        string memberName,
        string? message,
        string? errorCode = null,
        IDictionary<string, object>? messageParameters = null,
        ValidationErrorSeverity severity = ValidationErrorSeverity.Error,
        object? customState = null
    ) {
        MemberName = memberName;
        MessageId = MessageId.None;
        MessageKey = null;
        MessageParameters = messageParameters;
        Message = message;
        ErrorCode = errorCode;
        Severity = severity;
        CustomState = customState;
        MessageIsAuthored = message is not null;
    }
}
