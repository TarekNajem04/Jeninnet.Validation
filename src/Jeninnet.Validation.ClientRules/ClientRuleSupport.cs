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
internal static class ClientRuleSupport {
    private static readonly HashSet<string> _supportedRules = [
        with(StringComparer.Ordinal),
        "NotNull",
        "NotEmpty",
        "StringNotEmpty",
        "Length",
        "ExactLength",
        "Regex",
        "Email",
        "Url",
        "IPAddress",
        "Phone",
        "CreditCard",
        "Base64String",
        "FileExtensions",
        "Contains",
        "StartsWith",
        "EndsWith",
        "Count",
        "MinCount",
        "MaxCount",
        "UniqueItems",
        "MultipleOf",
        "GreaterThan",
        "GreaterThanOrEqual",
        "LessThan",
        "LessThanOrEqual",
        "InclusiveBetween",
        "ExclusiveBetween",
        "Range",
        "In",
        "NotIn",
        "ScalePrecision",
        "After",
        "Before"
    ];

    internal static bool IsServerOnly(IValidationRuleDescriptor rule) =>
        rule.HasCondition || !_supportedRules.Contains(rule.Kind);
}
