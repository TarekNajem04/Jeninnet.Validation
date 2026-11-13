//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// Represents a conditional rule that executes a group of inner rules only when a predicate is satisfied.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public sealed class ConditionalRule<T> : ISyncValidationRule<T>, IAsyncValidationRule<T>, IRuleMetadataProvider, IValidationMemberRule, IValidationRuleContainer {
    private readonly Func<T, bool>? _condition;
    private readonly Func<T, ValidationContext<T>, bool>? _contextCondition;
    private readonly Func<T, CancellationToken, Task<bool>>? _asyncCondition;
    private readonly ISyncValidationRule<T>[] _rules;
    private readonly IAsyncValidationRule<T>[] _asyncRules;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalRule{T}"/> class.
    /// </summary>
    /// <param name="condition">The synchronous condition. May be <see langword="null"/> if only an async condition is used.</param>
    /// <param name="asyncCondition">The asynchronous condition. May be <see langword="null"/> if only a sync condition is used.</param>
    /// <param name="rules">The synchronous rules to execute when the condition is satisfied.</param>
    /// <param name="asyncRules">The asynchronous rules to execute when the condition is satisfied.</param>
    public ConditionalRule(
        Func<T, bool>? condition,
        Func<T, CancellationToken, Task<bool>>? asyncCondition,
        IEnumerable<ISyncValidationRule<T>>? rules,
        IEnumerable<IAsyncValidationRule<T>>? asyncRules
    ) {
        if(condition is null && asyncCondition is null) {
            throw new ArgumentException("Either a synchronous or an asynchronous condition must be provided.");
        }

        _condition = condition;
        _asyncCondition = asyncCondition;
        _rules = [.. rules ?? []];
        _asyncRules = [.. asyncRules ?? []];
    }

    /// <summary>
    /// Initializes a conditional rule whose condition receives the validation context.
    /// </summary>
    /// <param name="condition">The synchronous context-aware condition.</param>
    /// <param name="rules">The synchronous rules to execute when the condition is satisfied.</param>
    /// <param name="asyncRules">The asynchronous rules to execute when the condition is satisfied.</param>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is null.</exception>
    public ConditionalRule(
        Func<T, ValidationContext<T>, bool> condition,
        IEnumerable<ISyncValidationRule<T>>? rules,
        IEnumerable<IAsyncValidationRule<T>>? asyncRules) {
        ArgumentNullException.ThrowIfNull(condition);

        _contextCondition = condition;
        _rules = [.. rules ?? []];
        _asyncRules = [.. asyncRules ?? []];
    }

    /// <inheritdoc />
    public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(_rules.Length is 0) {
            return;
        }

        if(_condition is null && _contextCondition is null) {
            // Purely async condition – this rule is effectively "no-op" in sync context.
            return;
        }

        var shouldRun = _contextCondition is not null
            ? _contextCondition(context.Instance, context)
            : _condition!(context.Instance);

        if(!shouldRun) {
            return;
        }

        for(var i = 0; i < _rules.Length; i++) {
            var rule = _rules[i];
            rule.Validate(in context, errors);
        }
    }

    /// <inheritdoc />
    public async ValueTask ValidateAsync(
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken = default
    ) {
        var hasSyncCondition = _condition is not null || _contextCondition is not null;
        var hasAsyncCondition = _asyncCondition is not null;

        if(!hasSyncCondition && !hasAsyncCondition) {
            return;
        }

        if(!await EvaluateConditionAsync(context, cancellationToken).ConfigureAwait(false)) {
            return;
        }

        if(_rules.Length > 0) {
            for(var i = 0; i < _rules.Length; i++) {
                var rule = _rules[i];
                rule.Validate(in context, errors);
            }
        }

        if(_asyncRules.Length > 0) {
            for(var i = 0; i < _asyncRules.Length; i++) {
                var rule = _asyncRules[i];
                await rule.ValidateAsync(context, errors, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    string? IValidationMemberRule.MemberName => GetSingleMemberName();

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

    IReadOnlyDictionary<string, object?>? IValidationRuleContainer.Parameters => null;

    bool IValidationRuleContainer.HasCondition => true;

    string? IValidationRuleContainer.MemberName => GetSingleMemberName();

    private async ValueTask<bool> EvaluateConditionAsync(
        ValidationContext<T> context,
        CancellationToken cancellationToken) {
        if(_asyncCondition is not null) {
            return await _asyncCondition(context.Instance, cancellationToken).ConfigureAwait(false);
        }

        if(_contextCondition is not null) {
            return _contextCondition(context.Instance, context);
        }

        return _condition!(context.Instance);
    }

    private string? GetSingleMemberName() =>
        CompositeRuleHelper.GetSingleMemberName(_rules, _asyncRules);

    string IRuleMetadataProvider.Kind => "Conditional";

    string? IRuleMetadataProvider.MessageKey => null;

    ValidationErrorSeverity IRuleMetadataProvider.Severity => ValidationErrorSeverity.Error;

    string? IRuleMetadataProvider.ErrorCode => null;

    string? IRuleMetadataProvider.DisplayName => null;

    object? IRuleMetadataProvider.CustomState => null;

    bool IRuleMetadataProvider.HasCondition => true;

    Type? IRuleMetadataProvider.MemberType => null;

    IReadOnlyDictionary<string, object?> IRuleMetadataProvider.GetMetadataParameters() => ValidationRuleDescriptor.EmptyParameters;
}
