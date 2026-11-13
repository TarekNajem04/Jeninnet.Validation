//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides an implementation of <see cref="IRuleBuilder{T, TProperty}"/> for a specific member.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <typeparam name="TProperty">The member type.</typeparam>
public sealed class RuleBuilder<T, TProperty> : IRuleBuilder<T, TProperty>, IRuleOptionsTargetBuilder, IRuleLevelCascadeModeProvider {
    private readonly MemberRuleGroup<T> _group;
    private readonly ValidatorRuleBuilder<T> _parentBuilder;
    private SyncValidateRule<T, TProperty>? _lastSyncRule;
    private AsyncValidateRule<T, TProperty>? _lastAsyncRule;
    private ISyncValidationRule<T>? _lastAddedSyncRule;
    private IAsyncValidationRule<T>? _lastAddedAsyncRule;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleBuilder{T, TProperty}"/> class.
    /// </summary>
    /// <param name="accessor">The property accessor for the member.</param>
    /// <param name="parentBuilder">The parent validator rule builder.</param>
    public RuleBuilder(IPropertyAccessor<T, TProperty> accessor, ValidatorRuleBuilder<T> parentBuilder) {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(parentBuilder);

        Accessor = accessor;
        _parentBuilder = parentBuilder;
        _group = new(memberName: accessor.MemberName);

        if(parentBuilder.RuleLevelCascadeModeOverride is CascadeMode explicitCascadeMode) {
            _group.CascadeMode = explicitCascadeMode;
        }

        parentBuilder.SyncRules.Add(_group);
        parentBuilder.AsyncRules.Add(_group);
    }

    /// <inheritdoc />
    public IPropertyAccessor<T, TProperty> Accessor { get; }

    /// <summary>
    /// Gets or sets the setter used to write sanitized values back to the member.
    /// Recorded by the <c>RuleFor</c> overload that accepts one; sanitization requires it.
    /// </summary>
    internal Action<T, TProperty?>? Setter { get; set; }

    /// <inheritdoc />
    public IRuleBuilder<T, TProperty> Sanitize(params Func<TProperty?, TProperty?>[] sanitizers) {
        ArgumentNullException.ThrowIfNull(sanitizers);

        if(sanitizers.Length is 0) {
            throw new ArgumentException("At least one sanitizer is required.", nameof(sanitizers));
        }

        var setter = Setter ?? throw new InvalidOperationException(
            $"Cannot sanitize member '{Accessor.MemberName}': the member was configured without a setter. " +
            "Use the RuleFor overload that accepts a setter.");

        _parentBuilder.AddSanitizer(Sanitizers.Member(Accessor.Getter, Accessor.MemberName, setter, sanitizers));
        return this;
    }

    /// <inheritdoc />
    public IRuleBuilder<T, TProperty> Sanitize(SanitizeOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        if(typeof(TProperty) != typeof(string)) {
            throw new InvalidOperationException(
                $"SanitizeOptions applies to string members, but '{Accessor.MemberName}' is '{typeof(TProperty).Name}'. " +
                "Use Sanitize with explicit transforms for other member types.");
        }

        Func<TProperty?, TProperty?>[] sanitizers =
            [.. options.Build().Select(static transform => (Func<TProperty?, TProperty?>)(object)transform)];
        return Sanitize(sanitizers);
    }

    /// <inheritdoc />
    public void AddRule(ISyncValidationRule<T> rule) {
        ArgumentNullException.ThrowIfNull(rule);

        _group.MutableSyncRules.Add(rule);
        _lastSyncRule = rule as SyncValidateRule<T, TProperty>;
        _lastAsyncRule = null;
        _lastAddedSyncRule = rule;
        _lastAddedAsyncRule = rule is IAsyncValidationRule<T> asyncRule && _group.MutableAsyncRules.Contains(asyncRule)
            ? asyncRule
            : null;
    }

    /// <inheritdoc />
    public void AddAsyncRule(IAsyncValidationRule<T> rule) {
        ArgumentNullException.ThrowIfNull(rule);

        _group.MutableAsyncRules.Add(rule);
        _lastAsyncRule = rule as AsyncValidateRule<T, TProperty>;
        _lastSyncRule = null;
        _lastAddedAsyncRule = rule;
        _lastAddedSyncRule = rule is ISyncValidationRule<T> syncRule && _group.MutableSyncRules.Contains(syncRule)
            ? syncRule
            : null;
    }

    /// <summary>
    /// Sets the cascade mode for this member's rule group.
    /// </summary>
    /// <param name="mode">The cascade mode to apply.</param>
    /// <returns>The current rule builder.</returns>
    public IRuleBuilder<T, TProperty> Cascade(CascadeMode mode) {
        _group.CascadeMode = mode;
        return this;
    }

    /// <inheritdoc />
    CascadeMode? IRuleLevelCascadeModeProvider.RuleLevelCascadeMode => _group.CascadeMode;

    internal void ConfigureLastSyncRule(Action<SyncValidateRule<T, TProperty>> configure) {
        if(_lastSyncRule is not null) {
            configure(_lastSyncRule);
        }
    }

    internal void ConfigureLastAsyncRule(Action<AsyncValidateRule<T, TProperty>> configure) {
        if(_lastAsyncRule is not null) {
            configure(_lastAsyncRule);
        }
    }

    internal void ConfigureLastRule(RuleOptionOverrides options) {
        ArgumentNullException.ThrowIfNull(options);

        var applied = false;

        if(_lastAddedSyncRule is IRuleOptionsTarget syncTarget) {
            syncTarget.ApplyOptions(options);
            applied = true;
        }

        if(_lastAddedAsyncRule is IRuleOptionsTarget asyncTarget &&
           !ReferenceEquals(_lastAddedSyncRule, _lastAddedAsyncRule)) {
            asyncTarget.ApplyOptions(options);
            applied = true;
        }

        if(!applied) {
            throw new InvalidOperationException(
                "Message/code/severity/state/name options must directly follow a rule that supports them. " +
                "Configure options before When/Unless/DependentRules modifiers.");
        }
    }

    void IRuleOptionsTargetBuilder.ConfigureLastRule(RuleOptionOverrides options) => ConfigureLastRule(options);

    internal void ApplyRuleSetToLastRule(string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var syncRule = _lastAddedSyncRule;
        var asyncRule = _lastAddedAsyncRule;
        var hasSync = syncRule is not null && _group.MutableSyncRules.Contains(syncRule);
        var hasAsync = asyncRule is not null && _group.MutableAsyncRules.Contains(asyncRule);

        if(!hasSync && !hasAsync) {
            throw new InvalidOperationException(
                "RuleSet must directly follow a rule created by RuleFor.");
        }

        IEnumerable<ISyncValidationRule<T>> syncRules = hasSync ? [syncRule!] : [];
        IEnumerable<IAsyncValidationRule<T>> asyncRules = hasAsync ? [asyncRule!] : [];
        var ruleSet = new RuleSetRule<T>([name], syncRules, asyncRules);

        if(hasSync) {
            _group.MutableSyncRules.Remove(syncRule!);
            _parentBuilder.SyncRules.Add(ruleSet);
        }

        if(hasAsync) {
            _group.MutableAsyncRules.Remove(asyncRule!);
            _parentBuilder.AsyncRules.Add(ruleSet);
        }

        _lastSyncRule = null;
        _lastAsyncRule = null;
        _lastAddedSyncRule = null;
        _lastAddedAsyncRule = null;
    }

    internal void ApplyForEachFilter<TElement>(Func<TElement, bool>? filter, Func<TElement, CancellationToken, ValueTask<bool>>? asyncFilter) {
        if(_lastAddedSyncRule is ForEachRule<T, TElement> syncRule) {
            ApplyForEach(syncRule, filter, asyncFilter);
            return;
        }

        if(_lastAddedAsyncRule is ForEachRule<T, TElement> asyncRule) {
            ApplyForEach(asyncRule, filter, asyncFilter);
            return;
        }

        if(_lastAddedSyncRule is ForEachElementRule<T, TElement> syncElementRule) {
            ApplyForEachElement(syncElementRule, filter, asyncFilter);
            return;
        }

        if(_lastAddedAsyncRule is ForEachElementRule<T, TElement> asyncElementRule) {
            ApplyForEachElement(asyncElementRule, filter, asyncFilter);
            return;
        }

        throw new InvalidOperationException("Where/WhereAsync must directly follow ForEach or ForEachElement.");
    }

    private void ApplyForEach<TElement>(
        ForEachRule<T, TElement> rule,
        Func<TElement, bool>? filter,
        Func<TElement, CancellationToken, ValueTask<bool>>? asyncFilter) {
        if(filter is not null) {
            rule.Where(filter);
        }

        if(asyncFilter is not null) {
            ParkOnAsyncList(rule);
            rule.WhereAsync(asyncFilter);
        }
    }

    private void ApplyForEachElement<TElement>(
        ForEachElementRule<T, TElement> rule,
        Func<TElement, bool>? filter,
        Func<TElement, CancellationToken, ValueTask<bool>>? asyncFilter) {
        if(filter is not null) {
            rule.Where(filter);
        }

        if(asyncFilter is not null) {
            ParkOnAsyncList(rule);
            rule.WhereAsync(asyncFilter);
        }
    }

    private void ParkOnAsyncList<TElement>(TElement rule)
        where TElement : ISyncValidationRule<T>, IAsyncValidationRule<T> {
        // An asynchronous filter is async-path-only: park the whole rule on the async
        // list so the synchronous phase does not validate unfiltered elements.
        _group.MutableSyncRules.Remove(rule);

        if(!_group.MutableAsyncRules.Contains(rule)) {
            _group.MutableAsyncRules.Add(rule);
        }
    }

    internal void ApplyForEachIndexer<TElement>(Func<T, int, string> indexer) {
        ArgumentNullException.ThrowIfNull(indexer);

        if(_lastAddedSyncRule is ForEachRule<T, TElement> syncRule) {
            syncRule.OverrideIndexer(indexer);
            return;
        }

        if(_lastAddedAsyncRule is ForEachRule<T, TElement> asyncRule) {
            asyncRule.OverrideIndexer(indexer);
            return;
        }

        if(_lastAddedSyncRule is ForEachElementRule<T, TElement> syncElementRule) {
            syncElementRule.OverrideIndexer(indexer);
            return;
        }

        if(_lastAddedAsyncRule is ForEachElementRule<T, TElement> asyncElementRule) {
            asyncElementRule.OverrideIndexer(indexer);
            return;
        }

        throw new InvalidOperationException("OverrideIndexer must directly follow ForEach or ForEachElement.");
    }

    internal IRuleBuilder<T, TProperty> AddDependentRules(Action<ValidatorRuleBuilder<T>> configure) {
        ArgumentNullException.ThrowIfNull(configure);

        var parentSync = _lastAddedSyncRule;
        var parentAsync = _lastAddedAsyncRule;

        if(parentSync is null && parentAsync is null) {
            throw new InvalidOperationException(
                "DependentRules must directly follow the rule they depend on.");
        }

        var dependentSyncRules = new List<ISyncValidationRule<T>>();
        var dependentAsyncRules = new List<IAsyncValidationRule<T>>();

        configure(new ValidatorRuleBuilder<T>(dependentSyncRules, dependentAsyncRules));

        _lastSyncRule = null;
        _lastAsyncRule = null;
        _lastAddedSyncRule = null;
        _lastAddedAsyncRule = null;

        if(dependentSyncRules.Count is 0 && dependentAsyncRules.Count is 0) {
            return this;
        }

        if(parentSync is not null && parentAsync is not null &&
           ReferenceEquals(parentSync, parentAsync)) {
            _group.MutableSyncRules.Remove(parentSync);
            _group.MutableAsyncRules.Remove(parentAsync);
        } else if(parentSync is not null) {
            _group.MutableSyncRules.Remove(parentSync);
        } else {
            _group.MutableAsyncRules.Remove(parentAsync!);
        }

        var dependent = new DependentRule<T>(parentSync, parentAsync, dependentSyncRules, dependentAsyncRules, _group);

        if(parentSync is not null) {
            _group.MutableSyncRules.Add(dependent);
        }

        if(parentAsync is not null || dependentAsyncRules.Count > 0) {
            _group.MutableAsyncRules.Add(dependent);
        }

        return this;
    }

    internal IRuleBuilder<T, TProperty> ApplyCondition(
        Func<T, bool>? syncCondition,
        Func<T, CancellationToken, Task<bool>>? asyncCondition,
        ApplyConditionTo applyTo) {
        if(syncCondition is null && asyncCondition is null) {
            throw new ArgumentException("Either a synchronous or an asynchronous condition must be provided.");
        }

        if(applyTo is ApplyConditionTo.AllValidators) {
            ApplyToAll(syncCondition, asyncCondition);
        } else {
            ApplyToLast(syncCondition, asyncCondition);
        }

        _lastSyncRule = null;
        _lastAsyncRule = null;
        _lastAddedSyncRule = null;
        _lastAddedAsyncRule = null;

        return this;
    }

    private void ApplyToLast(
        Func<T, bool>? syncCondition,
        Func<T, CancellationToken, Task<bool>>? asyncCondition) {
        var syncRule = _lastAddedSyncRule;
        var asyncRule = _lastAddedAsyncRule;

        if(syncRule is not null) {
            _group.MutableSyncRules.Remove(syncRule);

            var conditional = new ConditionalRule<T>(syncCondition, asyncCondition, [syncRule], null);

            if(syncCondition is not null) {
                _group.MutableSyncRules.Add(conditional);
            } else {
                _group.MutableAsyncRules.Add(conditional);
            }
        }

        if(asyncRule is not null) {
            _group.MutableAsyncRules.Remove(asyncRule);
            _group.MutableAsyncRules.Add(new ConditionalRule<T>(syncCondition, asyncCondition, null, [asyncRule]));
        }
    }

    private void ApplyToAll(
        Func<T, bool>? syncCondition,
        Func<T, CancellationToken, Task<bool>>? asyncCondition) {
        if(_group.MutableSyncRules.Count > 0) {
            var syncRules = _group.MutableSyncRules.ToArray();
            _group.MutableSyncRules.Clear();

            var conditional = new ConditionalRule<T>(syncCondition, asyncCondition, syncRules, null);

            if(syncCondition is not null) {
                _group.MutableSyncRules.Add(conditional);
            } else {
                _group.MutableAsyncRules.Add(conditional);
            }
        }

        if(_group.MutableAsyncRules.Count > 0) {
            var asyncRules = _group.MutableAsyncRules.ToArray();
            _group.MutableAsyncRules.Clear();
            _group.MutableAsyncRules.Add(new ConditionalRule<T>(syncCondition, asyncCondition, null, asyncRules));
        }
    }
}
