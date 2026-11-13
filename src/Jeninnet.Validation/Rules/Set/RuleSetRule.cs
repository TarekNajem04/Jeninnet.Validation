//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Represents a named group of validation rules.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public sealed class RuleSetRule<T> : IValidationRule<T>, IRuleMetadataProvider, IValidationMemberRule, IValidationRuleContainer {
    /// <summary>
    /// Gets the rule set names this group belongs to.
    /// </summary>
    public IReadOnlyList<string> Names { get; }

    private readonly ISyncValidationRule<T>[] _rules;
    private readonly IAsyncValidationRule<T>[] _asyncRules;

    internal IReadOnlyList<ISyncValidationRule<T>> SyncRules => _rules;

    internal IReadOnlyList<IAsyncValidationRule<T>> AsyncRules => _asyncRules;

    IEnumerable<object> IValidationRuleContainer.InnerRules {
        get {
            foreach(var rule in _rules) {
                yield return rule;
            }

            foreach(var rule in _asyncRules) {
                yield return rule;
            }
        }
    }

    bool IValidationRuleContainer.HasSyncContent => RuleContentInspector.HasSyncContent(_rules);

    IReadOnlyDictionary<string, object?>? IValidationRuleContainer.Parameters =>
        new Dictionary<string, object?> { { "RuleSets", string.Join(", ", Names) } };

    bool IValidationRuleContainer.HasCondition => false;

    string? IValidationRuleContainer.MemberName => GetSingleMemberName();

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleSetRule{T}"/> class with the specified names and rules.
    /// </summary>
    /// <param name="names">The rule set names this group belongs to. At least one non-empty name must be provided.</param>
    /// <param name="rules">Optional synchronous validation rules in this rule set.</param>
    /// <param name="asyncRules">Optional asynchronous validation rules in this rule set.</param>
    public RuleSetRule(
        IEnumerable<string> names,
        IEnumerable<ISyncValidationRule<T>>? rules,
        IEnumerable<IAsyncValidationRule<T>>? asyncRules
    ) {
        ArgumentNullException.ThrowIfNull(names);

        var nameArray = names.Where(static n => !string.IsNullOrWhiteSpace(n))
                             .Select(static n => n.Trim())
                             .ToArray();

        if(nameArray.Length == 0) {
            throw new ArgumentException("At least one rule set name must be provided.", nameof(names));
        }

        Names = nameArray;
        _rules = [.. rules ?? []];
        _asyncRules = [.. asyncRules ?? []];
    }

    /// <summary>
    /// Executes all synchronous validation rules in this rule set.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">The collector for validation errors.</param>
    public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
        foreach(var rule in _rules) {
            rule.Validate(in context, errors);
        }
    }

    /// <summary>
    /// Executes all asynchronous validation rules in this rule set.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="errors">The collector for validation errors.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async ValueTask ValidateAsync(
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken = default
    ) {
        for(var i = 0; i < _asyncRules.Length; i++) {
            var rule = _asyncRules[i];
            await rule.ValidateAsync(context, errors, cancellationToken).ConfigureAwait(false);
        }
    }

    string? IValidationMemberRule.MemberName => GetSingleMemberName();

    private string? GetSingleMemberName() =>
        CompositeRuleHelper.GetSingleMemberName(_rules, _asyncRules);

    string IRuleMetadataProvider.Kind => "RuleSet";

    string? IRuleMetadataProvider.MessageKey => null;

    ValidationErrorSeverity IRuleMetadataProvider.Severity => ValidationErrorSeverity.Error;

    string? IRuleMetadataProvider.ErrorCode => null;

    string? IRuleMetadataProvider.DisplayName => null;

    object? IRuleMetadataProvider.CustomState => null;

    bool IRuleMetadataProvider.HasCondition => false;

    Type? IRuleMetadataProvider.MemberType => null;

    IReadOnlyDictionary<string, object?> IRuleMetadataProvider.GetMetadataParameters() =>
        new Dictionary<string, object?> { { "RuleSets", string.Join(", ", Names) } };
}
