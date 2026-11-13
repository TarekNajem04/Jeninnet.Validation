//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Internal metadata bridge for rules that wrap other rules (member groups, rule sets,
/// conditions, and dependent blocks).
/// </summary>
internal interface IValidationRuleContainer {
    /// <summary>
    /// Gets the wrapped rules in declaration order.
    /// </summary>
    IEnumerable<object> InnerRules { get; }

    /// <summary>
    /// Gets a value indicating whether the container carries at least one rule that contributes
    /// synchronous validation content.
    /// </summary>
    bool HasSyncContent { get; }

    /// <summary>
    /// Gets optional parameters inherited by every wrapped rule.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Parameters { get; }

    /// <summary>
    /// Gets a value indicating whether the wrapped rules are conditional.
    /// </summary>
    bool HasCondition { get; }

    /// <summary>
    /// Gets the member name shared by the wrapped rules, when known.
    /// </summary>
    string? MemberName { get; }
}
