//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Abstractions;

/// <summary>
/// Mutable defaults exposed to <c>OnRuleAdded</c> so a validator can adjust message, error code,
/// and severity after a rule is fully configured.
/// </summary>
public interface IRuleDefinitionOptions {
    /// <summary>
    /// Gets or sets the custom error message for the rule.
    /// </summary>
    string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the machine-readable error code for the rule.
    /// </summary>
    string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the failure severity for the rule.
    /// </summary>
    ValidationErrorSeverity Severity { get; set; }
}
