//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation;

/// <summary>
/// A mutable collector for validation errors that can be passed to validation rules to accumulate errors without needing to allocate a new list for every rule.
/// </summary>
public sealed record ValidationErrorCollector : IReadOnlyList<ValidationError> {
    /// <summary>
    /// Gets a singleton empty instance.
    /// </summary>
    public static readonly ValidationErrorCollector Empty = [];

    private List<ValidationError>? _errors;

    /// <summary>
    /// Gets the validation error at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the error to retrieve.</param>
    /// <returns>The validation error at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when no errors have been added or the index is out of range.</exception>
    public ValidationError this[int index] =>
        _errors is null
            ? throw new ArgumentOutOfRangeException(nameof(index), "No errors have been added to the collector.")
            : _errors[index];

    /// <summary>
    /// Gets the number of validation errors in the collector.
    /// </summary>
    public int Count => _errors?.Count ?? 0;

    /// <summary>
    /// Adds a validation error with positional message parameters.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="messageId">The identifier for the validation message.</param>
    /// <param name="messageParameters">Positional parameters converted to dictionary format.</param>
    /// <param name="message">Optional override message text.</param>
    /// <param name="errorCode">Optional programmatic error code.</param>
    /// <param name="severity">The severity of the validation error.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(
        string memberName,
        MessageId messageId,
        object[]? messageParameters,
        string? message,
        string? errorCode,
        ValidationErrorSeverity severity
    ) {
        IDictionary<string, object>? dictParameters =
            messageParameters?.Select((p, i) => new KeyValuePair<string, object>(i.ToString(CultureInfo.InvariantCulture), p))
                              .ToDictionary(x => x.Key, x => x.Value);
        Add(memberName, messageId, null, dictParameters, message, errorCode, severity);
    }

    /// <summary>
    /// Adds a validation error with a message key and dictionary parameters.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="messageId">The identifier for the validation message.</param>
    /// <param name="messageKey">Key for looking up a localized message.</param>
    /// <param name="messageParameters">Dictionary of parameters for formatting the message.</param>
    /// <param name="message">Optional override message text.</param>
    /// <param name="errorCode">Optional programmatic error code.</param>
    /// <param name="severity">The severity of the validation error.</param>
    /// <param name="customState">Optional custom state object.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable S107 //Method has 8 parameters, which is greater than the 7 authorized.
    public void Add(
        string memberName,
        MessageId messageId,
        string? messageKey,
        IDictionary<string, object>? messageParameters,
        string? message,
        string? errorCode,
        ValidationErrorSeverity severity,
        object? customState = null
    ) => Add(new ValidationError(memberName, messageId, messageKey, messageParameters, message, errorCode, severity, customState));
#pragma warning restore S107

    /// <summary>
    /// Adds a validation error with severity and no message formatting.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="messageId">The identifier for the validation message.</param>
    /// <param name="message">Optional override message text.</param>
    /// <param name="errorCode">Optional programmatic error code.</param>
    /// <param name="severity">The severity of the validation error.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(
        string memberName,
        MessageId messageId,
        string? message,
        string? errorCode,
        ValidationErrorSeverity severity
    ) => Add(new ValidationError(memberName, messageId, messageKey: null, messageParameters: null, message, errorCode, severity));

    /// <summary>
    /// Adds a validation error with dictionary parameters and default Error severity.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="messageId">The identifier for the validation message.</param>
    /// <param name="messageParameters">Dictionary of parameters for formatting the message.</param>
    /// <param name="message">Optional override message text.</param>
    /// <param name="errorCode">Optional programmatic error code.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(
        string memberName,
        MessageId messageId,
        IDictionary<string, object>? messageParameters,
        string? message,
        string? errorCode
    ) => Add(new ValidationError(memberName, messageId, messageKey: null, messageParameters, message, errorCode, severity: ValidationErrorSeverity.Error));

    /// <summary>
    /// Adds a validation error with a message and error code, defaulting to Error severity.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="messageId">The identifier for the validation message.</param>
    /// <param name="message">Optional override message text.</param>
    /// <param name="errorCode">Optional programmatic error code.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(
        string memberName,
        MessageId messageId,
        string? message,
        string? errorCode
    ) => Add(new ValidationError(memberName, messageId, messageKey: null, messageParameters: null, message, errorCode, severity: ValidationErrorSeverity.Error));

    /// <summary>
    /// Adds a validation error with only a member name and message identifier.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="messageId">The identifier for the validation message.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(string memberName, MessageId messageId)
        => Add(new ValidationError(memberName, messageId, messageKey: null, messageParameters: null, message: null, errorCode: null, severity: ValidationErrorSeverity.Error));

    /// <summary>
    /// Adds a validation error with a message and severity, using <see cref="MessageId.None"/>.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="message">The validation error message.</param>
    /// <param name="severity">The severity of the validation error.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(string memberName, string? message, ValidationErrorSeverity severity)
        => Add(new ValidationError(memberName, MessageId.None, messageKey: null, messageParameters: null, message, errorCode: null, severity));

    /// <summary>
    /// Adds a validation error with a direct message, defaulting to Error severity.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="message">The validation error message.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(string memberName, string? message)
        => Add(new ValidationError(memberName, MessageId.None, messageKey: null, messageParameters: null, message, errorCode: null, severity: ValidationErrorSeverity.Error));

    /// <summary>
    /// Adds a pre-built validation error to the collector.
    /// </summary>
    /// <param name="error">The validation error to add.</param>
    public void Add(ValidationError error) {
        error = EnrichPathParameters(error);

        // Only allocate the list if an error actually occurs
        _errors ??= [];
        _errors.Add(error);
    }

    /// <summary>
    /// Adds a pre-built validation error and marks its message as authored.
    /// </summary>
    /// <param name="error">The validation error to add.</param>
    /// <param name="messageIsAuthored">Whether the message was authored explicitly.</param>
    public void Add(ValidationError error, bool messageIsAuthored) => Add(messageIsAuthored ? error with { MessageIsAuthored = true } : error);

    /// <summary>
    /// Adds a direct authored message with the specified severity.
    /// </summary>
    /// <param name="memberName">The name of the member that failed validation.</param>
    /// <param name="message">The authored validation error message.</param>
    /// <param name="severity">The severity of the validation error.</param>
    /// <param name="messageIsAuthored">Whether the message was authored explicitly.</param>
    public void Add(string memberName, string? message, ValidationErrorSeverity severity, bool messageIsAuthored) =>
        Add(new ValidationError(memberName, MessageId.None, messageKey: null, messageParameters: null, message, errorCode: null, severity) {
            MessageIsAuthored = messageIsAuthored
        });

    private static ValidationError EnrichPathParameters(ValidationError error) {
        if(error.MessageParameters?.ContainsKey(MessageParameterNames.PropertyPath) == true &&
           error.MessageParameters.ContainsKey(MessageParameterNames.PropertyName)) {
            return error;
        }

        var parameters = error.MessageParameters is null
            ? [with(StringComparer.Ordinal)]
            : new Dictionary<string, object>(error.MessageParameters, StringComparer.Ordinal);

        if(!parameters.ContainsKey(MessageParameterNames.PropertyName)) {
            parameters[MessageParameterNames.PropertyName] = error.MemberName;
        }

        parameters[MessageParameterNames.PropertyPath] = error.MemberName;

        return new ValidationError(
            error.MemberName,
            error.MessageId,
            error.MessageKey,
            parameters,
            error.Message,
            error.ErrorCode,
            error.Severity,
            error.CustomState) {
            MessageIsAuthored = error.MessageIsAuthored
        };
    }

    /// <summary>
    /// Adds multiple validation errors from a sequence.
    /// </summary>
    /// <param name="errors">The validation errors to add.</param>
    public void AddRange(IEnumerable<ValidationError> errors) {
        // Only allocate the list if an error actually occurs
        _errors ??= [];
        _errors.AddRange(errors);
    }

    /// <summary>
    /// Adds validation errors from another collector.
    /// </summary>
    /// <param name="other">The collector whose errors to add.</param>
    public void AddRange(ValidationErrorCollector other) {
        if(other?._errors is null) {
            return;
        }

        // Only allocate the list if an error actually occurs
        _errors ??= [];
        _errors.AddRange(other._errors);
    }

    /// <summary>
    /// Checks whether any errors have been added to the collector.
    /// </summary>
    /// <returns><see langword="true"/> if at least one error exists; otherwise, <see langword="false"/>.</returns>
    public bool HasErrors() => _errors is { Count: > 0 };

    /// <summary>
    /// Clears all errors from the collector.
    /// </summary>
    public void Clear() => _errors?.Clear();

    /// <summary>
    /// Returns a snapshot of the errors as an independent list.
    /// </summary>
    public IReadOnlyList<ValidationError> ToList() => _errors?.ToList() ?? [];

    /// <summary>
    /// Returns an enumerator that iterates through the validation errors.
    /// </summary>
    /// <returns>An enumerator for the collection of validation errors.</returns>
    public IEnumerator<ValidationError> GetEnumerator() => (_errors ?? (IEnumerable<ValidationError>)[]).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
