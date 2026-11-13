//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// A selector that executes every rule. This is the default behavior when no selector is
/// configured.
/// </summary>
public sealed class DefaultValidatorSelector : IValidatorSelector {
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static DefaultValidatorSelector Instance { get; } = new();

    /// <inheritdoc />
    public bool CanExecute<T>(IValidationRuleDescriptor rule, string memberName, ValidationContext<T> context) {
        _ = rule;
        _ = memberName;
        _ = context;

        return true;
    }
}
