//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Applies declarative rule metadata to a rule instance. Used by generated validators; fluent
/// users normally use the corresponding builder options.
/// </summary>
public static class ValidationRuleMetadata {
    /// <summary>
    /// Applies optional error code, severity, display name, member name, and static custom state
    /// to a rule.
    /// </summary>
    /// <typeparam name="TRule">The rule type.</typeparam>
    /// <param name="rule">The rule instance.</param>
    /// <param name="errorCode">The optional machine-readable error code.</param>
    /// <param name="severity">The optional failure severity.</param>
    /// <param name="displayName">The optional display name for <c>{PropertyName}</c>.</param>
    /// <param name="memberName">The optional code-facing member name override.</param>
    /// <param name="customState">The optional static custom state.</param>
    /// <returns>The same rule instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is null.</exception>
    public static TRule Apply<TRule>(
        TRule rule,
        string? errorCode = null,
        ValidationErrorSeverity? severity = null,
        string? displayName = null,
        string? memberName = null,
        object? customState = null) where TRule : class {
        ArgumentNullException.ThrowIfNull(rule);

        if(errorCode is null && severity is null && displayName is null && memberName is null && customState is null) {
            return rule;
        }

        if(rule is not IRuleOptionsTarget target) {
            return rule;
        }

        var options = new RuleOptionOverrides();

        if(errorCode is not null) {
            options.ErrorCode = errorCode;
            options.HasErrorCode = true;
        }

        if(severity is not null) {
            options.Severity = severity.Value;
            options.HasSeverity = true;
        }

        if(displayName is not null) {
            options.DisplayName = displayName;
            options.HasDisplayName = true;
        }

        if(memberName is not null) {
            options.MemberName = memberName;
            options.HasMemberName = true;
        }

        if(customState is not null) {
            options.CustomState = customState;
            options.HasCustomState = true;
        }

        target.ApplyOptions(options);
        return rule;
    }
}
