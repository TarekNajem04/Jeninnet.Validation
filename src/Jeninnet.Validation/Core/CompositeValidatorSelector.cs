//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Combines several selectors; a rule executes when any contained selector selects it.
/// </summary>
public sealed class CompositeValidatorSelector : IValidatorSelector {
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeValidatorSelector"/> class.
    /// </summary>
    /// <param name="selectors">The selectors to combine.</param>
    /// <exception cref="ArgumentNullException"><paramref name="selectors"/> is null.</exception>
    public CompositeValidatorSelector(params IValidatorSelector[] selectors) {
        ArgumentNullException.ThrowIfNull(selectors);

        Selectors = [.. selectors.Where(static selector => selector is not null)];
    }

    /// <summary>
    /// Gets the combined selectors.
    /// </summary>
    public IReadOnlyList<IValidatorSelector> Selectors { get; }

    /// <inheritdoc />
    public bool CanExecute<T>(IValidationRuleDescriptor rule, string memberName, ValidationContext<T> context) =>
        Selectors.Any(selector => selector.CanExecute(rule, memberName, context));
}
