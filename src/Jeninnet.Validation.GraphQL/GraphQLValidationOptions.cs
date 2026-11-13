//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.GraphQL;

/// <summary>
/// Configures the HotChocolate GraphQL input validation middleware.
/// </summary>
public sealed class GraphQLValidationOptions {
    /// <summary>
    /// Gets the default rule sets applied when an input type has no
    /// <see cref="GraphQLValidationRuleSetsAttribute"/>. Empty means all rules.
    /// </summary>
    public IList<string> RuleSets { get; } = [];
}
