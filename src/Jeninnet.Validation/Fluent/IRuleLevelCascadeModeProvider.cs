//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Exposes the rule-level cascade mode inherited by nested rule builders.
/// </summary>
internal interface IRuleLevelCascadeModeProvider {
    /// <summary>
    /// Gets the explicit rule-level cascade mode for rules created by the builder, or
    /// <see langword="null"/> to inherit the effective options at validation time.
    /// </summary>
    CascadeMode? RuleLevelCascadeMode { get; }
}
