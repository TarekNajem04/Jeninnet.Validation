//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Marks a controller or action as excluded from automatic Jeninnet validation.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = true)]
public sealed class SkipJeninnetValidationAttribute : Attribute;

/// <summary>
/// Disables Jeninnet validation for an endpoint, handler method, or parameter.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = true)]
public sealed class DisableValidationAttribute : Attribute;

/// <summary>
/// Selects the rule sets used by automatic MVC validation for a controller or action.
/// </summary>
/// <param name="ruleSets">The rule-set names.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class JeninnetValidationRuleSetsAttribute(params string[] ruleSets) : Attribute {
    /// <summary>
    /// Gets the selected rule-set names.
    /// </summary>
    public IReadOnlyList<string> RuleSets { get; } = ruleSets;
}
