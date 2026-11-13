//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Exposes the member a rule belongs to so per-call property filters can target rules that are
/// not wrapped in a <see cref="MemberRuleGroup{T}"/> (notably generated flat rules).
/// </summary>
public interface IValidationMemberRule {
    /// <summary>
    /// Gets the member name, or <see langword="null"/> when the rule spans multiple members.
    /// </summary>
    string? MemberName { get; }
}
