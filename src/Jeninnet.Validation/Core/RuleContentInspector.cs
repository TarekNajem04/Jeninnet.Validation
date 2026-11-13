//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Inspects rule containers to determine whether a rule list carries synchronous content.
/// Member groups and wrappers can contain groups with empty synchronous lists, so the presence
/// of an <see cref="ISyncValidationRule{T}"/> alone is not enough.
/// </summary>
internal static class RuleContentInspector {
    internal static bool HasSyncContent<T>(IEnumerable<ISyncValidationRule<T>>? rules) {
        if(rules is null) {
            return false;
        }

        foreach(var rule in rules) {
            if(rule is IValidationRuleContainer container) {
                if(container.HasSyncContent) {
                    return true;
                }

                continue;
            }

            return true;
        }

        return false;
    }
}
