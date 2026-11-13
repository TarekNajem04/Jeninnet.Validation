//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Selects which top-level validation rules execute during a validation call. Selectors run after
/// rule-set and include/exclude property filtering.
/// </summary>
/// <remarks>
/// A top-level rule is a member rule group, a rule-set wrapper, a conditional/dependent wrapper, or
/// a model-level rule. Selectors are opt-in; when none is configured the engine keeps its
/// allocation-free default path.
/// </remarks>
public interface IValidatorSelector {
    /// <summary>
    /// Determines whether the supplied rule should execute.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="rule">The descriptor of the top-level rule.</param>
    /// <param name="memberName">The full member path associated with the rule, or an empty string for model-level rules.</param>
    /// <param name="context">The current validation context.</param>
    /// <returns><see langword="true"/> to execute the rule; otherwise <see langword="false"/>.</returns>
    bool CanExecute<T>(IValidationRuleDescriptor rule, string memberName, ValidationContext<T> context);
}
