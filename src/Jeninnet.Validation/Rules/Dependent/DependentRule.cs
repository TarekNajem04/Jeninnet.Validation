//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Rules;

/// <summary>
/// A rule group that executes only when the rule it is attached to produced no errors. The parent
/// runs first; dependent rules run afterwards in declaration order. Nested dependent blocks compose
/// recursively, and errors are emitted parent-first.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <remarks>
/// Synchronous dependents run in the synchronous phase; asynchronous dependents run in the
/// asynchronous phase only. When a synchronous parent has asynchronous dependents, the
/// synchronous phase records the gate in the per-walk <see cref="RootContextData"/> so the
/// asynchronous phase can read it without re-running the parent. A validator whose only content is
/// an asynchronous parent still throws on the synchronous path (the parent never runs there).
/// </remarks>
public sealed class DependentRule<T> : IValidationRule<T>, IRuleMetadataProvider, IValidationMemberRule, IValidationRuleContainer {
    private static readonly object _passedState = new();
    private static readonly object _failedState = new();

    private readonly ISyncValidationRule<T>? _parentSync;
    private readonly IAsyncValidationRule<T>? _parentAsync;
    private readonly ISyncValidationRule<T>[] _syncRules;
    private readonly IAsyncValidationRule<T>[] _asyncRules;
    private readonly MemberRuleGroup<T> _parentGroup;
    private readonly object _stateKey = new();
    private readonly bool _tracksState;

    /// <summary>
    /// Initializes a new instance of the <see cref="DependentRule{T}"/> class.
    /// </summary>
    /// <param name="parentSync">The synchronous parent rule, or <see langword="null"/> when the parent is asynchronous.</param>
    /// <param name="parentAsync">The asynchronous parent rule, or <see langword="null"/> when the parent is synchronous.</param>
    /// <param name="syncRules">The synchronous dependent rules.</param>
    /// <param name="asyncRules">The asynchronous dependent rules.</param>
    /// <param name="parentGroup">The member group that owns the parent rule; its cascade mode governs the dependent block.</param>
    /// <exception cref="ArgumentException">Both <paramref name="parentSync"/> and <paramref name="parentAsync"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="parentGroup"/> is null.</exception>
    public DependentRule(
        ISyncValidationRule<T>? parentSync,
        IAsyncValidationRule<T>? parentAsync,
        IEnumerable<ISyncValidationRule<T>>? syncRules,
        IEnumerable<IAsyncValidationRule<T>>? asyncRules,
        MemberRuleGroup<T> parentGroup) {
        if(parentSync is null && parentAsync is null) {
            throw new ArgumentException("A dependent rule requires a parent rule.");
        }

        ArgumentNullException.ThrowIfNull(parentGroup);

        _parentSync = parentSync;
        _parentAsync = parentAsync;
        _syncRules = [.. syncRules ?? []];
        _asyncRules = [.. asyncRules ?? []];
        _parentGroup = parentGroup;
        _tracksState = parentSync is not null && HasAsyncContent(_asyncRules);
    }

    /// <inheritdoc />
    public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);

        if(_parentSync is null) {
            // The parent is asynchronous and never runs on the synchronous path.
            return;
        }

        var countBefore = errors.Count;
        _parentSync.Validate(in context, errors);

        var passed = errors.Count == countBefore;

        if(_tracksState) {
            StorePassed(context, passed);
        }

        if(!passed) {
            return;
        }

        ValidateSyncRules(in context, errors);
    }

    /// <inheritdoc />
    public async ValueTask ValidateAsync(
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(errors);

        if(_parentAsync is not null) {
            var countBefore = errors.Count;
            await _parentAsync.ValidateAsync(context, errors, cancellationToken).ConfigureAwait(false);

            if(errors.Count > countBefore) {
                return;
            }

            ValidateSyncRules(in context, errors);
            await ValidateAsyncRulesAsync(context, errors, cancellationToken).ConfigureAwait(false);
            return;
        }

        // The synchronous phase stored the gate for this walk. If it is absent (the parent was
        // skipped by a cascade stop, or the rule was invoked directly), the dependents do not run.
        if(!_tracksState || !TryGetPassed(context, out var passed) || !passed) {
            return;
        }

        await ValidateAsyncRulesAsync(context, errors, cancellationToken).ConfigureAwait(false);
    }

    private void ValidateSyncRules(in ValidationContext<T> context, ValidationErrorCollector errors) {
        if(_syncRules.Length is 0) {
            return;
        }

        var stopOnError = StopOnError(in context);

        for(var i = 0; i < _syncRules.Length; i++) {
            var countBefore = errors.Count;
            _syncRules[i].Validate(in context, errors);

            if(stopOnError && errors.Count > countBefore) {
                return;
            }
        }
    }

    private async ValueTask ValidateAsyncRulesAsync(
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken) {
        if(_asyncRules.Length is 0) {
            return;
        }

        var stopOnError = StopOnError(in context);

        for(var i = 0; i < _asyncRules.Length; i++) {
            var countBefore = errors.Count;
            await _asyncRules[i].ValidateAsync(context, errors, cancellationToken).ConfigureAwait(false);

            if(stopOnError && errors.Count > countBefore) {
                return;
            }
        }
    }

    private bool StopOnError(in ValidationContext<T> context) =>
        context.FailFast ||
        (_parentGroup.CascadeMode ??
         context.Options?.DefaultRuleLevelCascadeMode ??
         ValidationConfiguration.Options.DefaultRuleLevelCascadeMode) is CascadeMode.Stop;

    private static bool HasAsyncContent(IAsyncValidationRule<T>[] rules) {
        foreach(var rule in rules) {
            if(rule is not MemberRuleGroup<T> group || group.AsyncRules.Count > 0) {
                return true;
            }
        }

        return false;
    }

    private void StorePassed(in ValidationContext<T> context, bool passed) =>
        context.RootData?.SetState(_stateKey, passed ? _passedState : _failedState);

    private bool TryGetPassed(in ValidationContext<T> context, out bool passed) {
        passed = false;

        if(context.RootData is null || !context.RootData.TryGetState(_stateKey, out var state)) {
            return false;
        }

        passed = ReferenceEquals(state, _passedState);
        return true;
    }

    string? IValidationMemberRule.MemberName => _parentGroup.MemberName;

    IEnumerable<object> IValidationRuleContainer.InnerRules {
        get {
            if(_parentSync is not null) {
                yield return _parentSync;
            } else if(_parentAsync is not null) {
                yield return _parentAsync;
            }

            foreach(var rule in _syncRules) {
                yield return rule;
            }

            foreach(var rule in _asyncRules) {
                yield return rule;
            }
        }
    }

    bool IValidationRuleContainer.HasSyncContent => _parentSync is not null;

    IReadOnlyDictionary<string, object?>? IValidationRuleContainer.Parameters =>
        new Dictionary<string, object?> { { "Dependent", true } };

    bool IValidationRuleContainer.HasCondition => false;

    string? IValidationRuleContainer.MemberName => _parentGroup.MemberName;

    string IRuleMetadataProvider.Kind => "Dependent";

    string? IRuleMetadataProvider.MessageKey => null;

    ValidationErrorSeverity IRuleMetadataProvider.Severity => ValidationErrorSeverity.Error;

    string? IRuleMetadataProvider.ErrorCode => null;

    string? IRuleMetadataProvider.DisplayName => null;

    object? IRuleMetadataProvider.CustomState => null;

    bool IRuleMetadataProvider.HasCondition => false;

    Type? IRuleMetadataProvider.MemberType => null;

    IReadOnlyDictionary<string, object?> IRuleMetadataProvider.GetMetadataParameters() => ValidationRuleDescriptor.EmptyParameters;
}
