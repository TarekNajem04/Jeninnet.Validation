//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Specifies which rules a per-rule condition applies to.
/// </summary>
public enum ApplyConditionTo {
    /// <summary>
    /// Apply the condition to the rule that was just configured (default).
    /// </summary>
    CurrentValidator = 0,

    /// <summary>
    /// Apply the condition to every rule already configured for the current member.
    /// </summary>
    AllValidators = 1
}
