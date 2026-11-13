//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A reusable instance-aware validation template. The predicate receives the model instance and
/// the member value.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TValue">The member value type.</typeparam>
public sealed class RuleTemplate<T, TValue> : RuleTemplate {

    /// <summary>
    /// Initializes a new instance-aware rule template.
    /// </summary>
    /// <param name="name">The stable template name.</param>
    /// <param name="predicate">The predicate; a <see langword="false"/> result emits a failure.</param>
    /// <param name="message">The optional fallback message.</param>
    /// <param name="messageKey">The optional message key.</param>
    /// <param name="severity">The default failure severity.</param>
    /// <param name="errorCode">The optional machine-readable error code.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    public RuleTemplate(
        string name,
        Func<T, TValue, bool> predicate,
        string? message = null,
        string? messageKey = null,
        ValidationErrorSeverity severity = ValidationErrorSeverity.Error,
        string? errorCode = null)
        : this(
            name,
            predicate,
            message,
            messageKey,
            severity,
            errorCode,
            ValidationRuleDescriptor.EmptyParameters,
            new Dictionary<string, Func<T, TValue, object?>>(StringComparer.Ordinal)) {
    }

    private RuleTemplate(
        string name,
        Func<T, TValue, bool> predicate,
        string? message,
        string? messageKey,
        ValidationErrorSeverity severity,
        string? errorCode,
        IReadOnlyDictionary<string, object?> parameters,
        IReadOnlyDictionary<string, Func<T, TValue, object?>> dynamicParameters)
        : base(name, message, messageKey, severity, errorCode, parameters, dynamicParameters.Keys) {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(dynamicParameters);

        Predicate = predicate;
        DynamicParameters = new ReadOnlyDictionary<string, Func<T, TValue, object?>>(
            new Dictionary<string, Func<T, TValue, object?>>(dynamicParameters, StringComparer.Ordinal));
    }

    /// <summary>
    /// Gets the predicate evaluated against the model instance and member value.
    /// </summary>
    internal Func<T, TValue, bool> Predicate { get; }

    /// <summary>
    /// Gets the value-dependent message-parameter factories.
    /// </summary>
    internal IReadOnlyDictionary<string, Func<T, TValue, object?>> DynamicParameters { get; }

    /// <summary>
    /// Returns a copy with a different fallback message.
    /// </summary>
    /// <param name="message">The fallback message.</param>
    /// <returns>A new template.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
    public RuleTemplate<T, TValue> WithMessage(string message) {
        ArgumentNullException.ThrowIfNull(message);

        return new RuleTemplate<T, TValue>(Name, Predicate, message, MessageKey, Severity, ErrorCode, Parameters, DynamicParameters);
    }

    /// <summary>
    /// Returns a copy with a different message key.
    /// </summary>
    /// <param name="messageKey">The message key.</param>
    /// <returns>A new template.</returns>
    /// <exception cref="ArgumentException"><paramref name="messageKey"/> is empty or whitespace.</exception>
    public RuleTemplate<T, TValue> WithMessageKey(string messageKey) {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageKey);

        return new RuleTemplate<T, TValue>(Name, Predicate, Message, messageKey, Severity, ErrorCode, Parameters, DynamicParameters);
    }

    /// <summary>
    /// Returns a copy with a different default severity.
    /// </summary>
    /// <param name="severity">The default severity.</param>
    /// <returns>A new template.</returns>
    public RuleTemplate<T, TValue> WithSeverity(ValidationErrorSeverity severity) =>
        new(Name, Predicate, Message, MessageKey, severity, ErrorCode, Parameters, DynamicParameters);

    /// <summary>
    /// Returns a copy with a different error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <returns>A new template.</returns>
    /// <exception cref="ArgumentException"><paramref name="errorCode"/> is empty or whitespace.</exception>
    public RuleTemplate<T, TValue> WithErrorCode(string errorCode) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        return new RuleTemplate<T, TValue>(Name, Predicate, Message, MessageKey, Severity, errorCode, Parameters, DynamicParameters);
    }

    /// <summary>
    /// Returns a copy with a static named parameter. A parameter with the same name replaces the
    /// previous static or dynamic value.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>A new template.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace.</exception>
    public RuleTemplate<T, TValue> WithParameter(string name, object? value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var parameters = new Dictionary<string, object?>(Parameters, StringComparer.Ordinal) {
            [name] = value
        };
        var dynamicParameters = new Dictionary<string, Func<T, TValue, object?>>(DynamicParameters, StringComparer.Ordinal);
        dynamicParameters.Remove(name);

        return new RuleTemplate<T, TValue>(
            Name,
            Predicate,
            Message,
            MessageKey,
            Severity,
            ErrorCode,
            parameters,
            dynamicParameters);
    }

    /// <summary>
    /// Returns a copy with a value-dependent named parameter. The factory runs only when the rule
    /// fails. A parameter with the same name replaces the previous static or dynamic value.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="valueFactory">The value factory; it receives the model and member value.</param>
    /// <returns>A new template.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is null.</exception>
    public RuleTemplate<T, TValue> WithParameter(string name, Func<T, TValue, object?> valueFactory) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(valueFactory);

        var parameters = new Dictionary<string, object?>(Parameters, StringComparer.Ordinal);
        parameters.Remove(name);
        var dynamicParameters = new Dictionary<string, Func<T, TValue, object?>>(DynamicParameters, StringComparer.Ordinal) {
            [name] = valueFactory
        };

        return new RuleTemplate<T, TValue>(
            Name,
            Predicate,
            Message,
            MessageKey,
            Severity,
            ErrorCode,
            parameters,
            dynamicParameters);
    }

    /// <summary>
    /// Returns a copy with several static named parameters.
    /// </summary>
    /// <param name="parameters">The parameters to add or replace.</param>
    /// <returns>A new template.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is null.</exception>
    /// <exception cref="ArgumentException">A parameter name is empty or whitespace.</exception>
    public RuleTemplate<T, TValue> WithParameters(IEnumerable<KeyValuePair<string, object?>> parameters) {
        ArgumentNullException.ThrowIfNull(parameters);

        var parameterCopy = new Dictionary<string, object?>(Parameters, StringComparer.Ordinal);
        var dynamicParameters = new Dictionary<string, Func<T, TValue, object?>>(DynamicParameters, StringComparer.Ordinal);

        foreach(var parameter in parameters) {
            ArgumentException.ThrowIfNullOrWhiteSpace(parameter.Key);
            parameterCopy[parameter.Key] = parameter.Value;
            dynamicParameters.Remove(parameter.Key);
        }

        return new RuleTemplate<T, TValue>(
            Name,
            Predicate,
            Message,
            MessageKey,
            Severity,
            ErrorCode,
            parameterCopy,
            dynamicParameters);
    }
}
