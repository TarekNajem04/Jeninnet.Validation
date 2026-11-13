//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Describes a reusable validation rule template: a name, a failure message, a message key,
/// severity, error code, and named parameters. Templates are immutable; the <c>With*</c> methods
/// return a new instance. Apply a template with <c>RuleFor(...).Rule(template)</c>.
/// </summary>
/// <remarks>
/// Templates are a fluent-authoring feature. They are not discoverable by the source generator
/// because the predicate is a runtime delegate; use attributes for generated validators.
/// </remarks>
public abstract class RuleTemplate {
    private protected RuleTemplate(
        string name,
        string? message,
        string? messageKey,
        ValidationErrorSeverity severity,
        string? errorCode,
        IReadOnlyDictionary<string, object?> parameters,
        IEnumerable<string> dynamicParameterNames) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(dynamicParameterNames);

        if(messageKey is not null) {
            ArgumentException.ThrowIfNullOrWhiteSpace(messageKey);
        }

        if(errorCode is not null) {
            ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        }

        var parameterCopy = new Dictionary<string, object?>(parameters.Count, StringComparer.Ordinal);

        foreach(var parameter in parameters) {
            ArgumentException.ThrowIfNullOrWhiteSpace(parameter.Key);
            parameterCopy[parameter.Key] = parameter.Value;
        }

        var metadataCopy = new Dictionary<string, object?>(parameterCopy, StringComparer.Ordinal);

        foreach(var dynamicParameterName in dynamicParameterNames) {
            ArgumentException.ThrowIfNullOrWhiteSpace(dynamicParameterName);
            metadataCopy.TryAdd(dynamicParameterName, null);
        }

        Name = name;
        Message = message;
        MessageKey = messageKey;
        Severity = severity;
        ErrorCode = errorCode;
        Parameters = new ReadOnlyDictionary<string, object?>(parameterCopy);
        MetadataParameters = new ReadOnlyDictionary<string, object?>(metadataCopy);
    }

    /// <summary>
    /// Gets the stable template name used as the descriptor kind and client-rule name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional fallback message. The localization pipeline may replace it by
    /// <see cref="MessageKey"/>.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the optional message key used for localization.
    /// </summary>
    public string? MessageKey { get; }

    /// <summary>
    /// Gets the default failure severity. A per-rule <c>WithSeverity</c> call overrides it.
    /// </summary>
    public ValidationErrorSeverity Severity { get; }

    /// <summary>
    /// Gets the optional machine-readable error code. A per-rule <c>WithCode</c> call overrides it.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the static named parameters used to format the failure message.
    /// </summary>
    /// <remarks>
    /// Value-dependent parameters are evaluated only when the rule fails. Their names are visible
    /// in the descriptor, but this dictionary contains static parameters only.
    /// </remarks>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Gets the parameters exposed by the descriptor. Dynamic parameter names are present with a
    /// <see langword="null"/> value because they cannot be evaluated without a model instance.
    /// </summary>
    internal IReadOnlyDictionary<string, object?> MetadataParameters { get; }
}
