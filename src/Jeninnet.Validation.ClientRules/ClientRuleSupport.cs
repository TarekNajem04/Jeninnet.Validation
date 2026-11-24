//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.ClientRules;

/// <summary>
/// Declares which descriptor rules the browser-side runtime can evaluate. Everything else is
/// explicitly marked <c>serverOnly</c> in the exported payload.
/// </summary>
internal static partial class ClientRuleSupport {
    internal static bool IsServerOnly(IValidationRuleDescriptor rule) =>
        rule.HasCondition || !_supportedRules.Contains(rule.Kind);
}
