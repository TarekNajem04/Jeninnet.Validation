//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Internal bridge that exposes rule metadata to the descriptor factory without reflection.
/// </summary>
internal interface IRuleMetadataProvider {
    /// <summary>
    /// Gets the stable rule kind.
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Gets the message key, when configured.
    /// </summary>
    string? MessageKey { get; }

    /// <summary>
    /// Gets a value indicating whether the rule carries an explicitly authored message.
    /// </summary>
    bool MessageIsAuthored => false;

    /// <summary>
    /// Gets the failure severity.
    /// </summary>
    ValidationErrorSeverity Severity { get; }

    /// <summary>
    /// Gets the optional machine-readable error code.
    /// </summary>
    string? ErrorCode { get; }

    /// <summary>
    /// Gets the optional display name for the <c>{PropertyName}</c> parameter.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Gets the optional static custom state.
    /// </summary>
    object? CustomState { get; }

    /// <summary>
    /// Gets a value indicating whether the rule is gated by a per-rule condition.
    /// </summary>
    bool HasCondition { get; }

    /// <summary>
    /// Gets the declared member type, when known.
    /// </summary>
    Type? MemberType { get; }

    /// <summary>
    /// Gets the rule parameters.
    /// </summary>
    /// <returns>The metadata parameters.</returns>
    IReadOnlyDictionary<string, object?> GetMetadataParameters();
}

/// <summary>
/// Implemented by a rule that supplies a dynamic rule kind instead of deriving it from its type.
/// </summary>
internal interface IRuleMetadataKindProvider {
    /// <summary>
    /// Gets the stable rule kind.
    /// </summary>
    string Kind { get; }
}

/// <summary>
/// Derives stable rule kind names from rule types.
/// </summary>
internal static class RuleMetadataKind {
    /// <summary>
    /// Gets the rule kind for a type (for example <c>GreaterThanRule</c> becomes
    /// <c>GreaterThan</c>).
    /// </summary>
    /// <param name="type">The rule type.</param>
    /// <returns>The stable kind name.</returns>
    public static string FromType(Type type) {
        ArgumentNullException.ThrowIfNull(type);

        var name = type.Name;
        var arity = name.IndexOf('`');

        if(arity >= 0) {
            name = name[..arity];
        }

        var kind = name.EndsWith("Rule", StringComparison.Ordinal) ? name[..^4] : name;

        return kind switch {
            "StringNotEmpty" => "NotEmpty",
            "RegexMatch" => "Regex",
            _ => kind
        };
    }
}
