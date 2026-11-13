//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Shared helpers for composite rules that wrap synchronous and asynchronous child rules.
/// </summary>
internal static class CompositeRuleHelper {
    /// <summary>
    /// Gets the member name when the composite contains exactly one member rule; otherwise null.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="syncRules">The synchronous child rules.</param>
    /// <param name="asyncRules">The asynchronous child rules.</param>
    internal static string? GetSingleMemberName<T>(
        ISyncValidationRule<T>[] syncRules,
        IAsyncValidationRule<T>[] asyncRules) {
        if(syncRules.Length is 1 && syncRules[0] is IValidationMemberRule syncMember) {
            return syncMember.MemberName;
        }

        if(asyncRules.Length is 1 && asyncRules[0] is IValidationMemberRule asyncMember) {
            return asyncMember.MemberName;
        }

        return null;
    }
}
