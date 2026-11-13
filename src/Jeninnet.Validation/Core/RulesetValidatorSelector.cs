//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Selects rules whose rule-set metadata intersects the configured rule-set names. A rule without
/// rule-set metadata is treated as part of the <c>default</c> set.
/// </summary>
public sealed class RulesetValidatorSelector : IValidatorSelector {
    /// <summary>
    /// Initializes a new instance of the <see cref="RulesetValidatorSelector"/> class.
    /// </summary>
    /// <param name="ruleSets">The rule-set names to select.</param>
    /// <exception cref="ArgumentNullException"><paramref name="ruleSets"/> is null.</exception>
    public RulesetValidatorSelector(params string[] ruleSets) {
        ArgumentNullException.ThrowIfNull(ruleSets);

        RuleSets = [.. ruleSets
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name.Trim())];
    }

    /// <summary>
    /// Gets the configured rule-set names.
    /// </summary>
    public IReadOnlyList<string> RuleSets { get; }

    /// <inheritdoc />
    public bool CanExecute<T>(IValidationRuleDescriptor rule, string memberName, ValidationContext<T> context) {
        _ = memberName;
        _ = context;
        ArgumentNullException.ThrowIfNull(rule);

        if(rule.Parameters.TryGetValue("RuleSets", out var value) && value is not null) {
            return ContainsAny(ReadRuleSets(value));
        }

        return ContainsAny(["default"]);
    }

    private bool ContainsAny(IEnumerable<string> names) =>
        names.Any(name => RuleSets.Contains(name, StringComparer.OrdinalIgnoreCase));

    private static IEnumerable<string> ReadRuleSets(object value) => value switch {
        string text => text.Split([','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        IEnumerable<string> names => names,
        _ => []
    };
}
