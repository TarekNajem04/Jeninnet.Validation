//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.GraphQL;

/// <summary>
/// Selects the rule sets used to validate a GraphQL input object type.
/// </summary>
/// <param name="ruleSets">The rule-set names.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class GraphQLValidationRuleSetsAttribute(params string[] ruleSets) : Attribute {
    /// <summary>
    /// Gets the selected rule-set names.
    /// </summary>
    public IReadOnlyList<string> RuleSets { get; } = ruleSets;
}
