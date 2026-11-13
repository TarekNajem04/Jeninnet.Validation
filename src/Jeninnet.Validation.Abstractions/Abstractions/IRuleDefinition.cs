//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Abstractions;

/// <summary>
/// A stable contract representing the metadata for a validation rule.
/// This allows the Source Generator to emit metadata objects instead of concrete rule types.
/// </summary>
public interface IRuleDefinition {
    /// <summary>
    /// Gets the custom error message for the rule.
    /// </summary>
    string? ErrorMessage { get; }
}

/// <summary>
/// Extends <see cref="IRuleDefinition"/> for rules applied to properties.
/// </summary>
public interface IPropertyRuleDefinition : IRuleDefinition {
    /// <summary>
    /// Gets the name of the property being validated.
    /// </summary>
    string PropertyName { get; }
}
