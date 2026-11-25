//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
using System.Diagnostics;

namespace Jeninnet.Validation.Core;

/// <summary>
/// Limits a trace walk to the phase that can execute the rule. <see cref="All"/> is used when a
/// wrapper runs both sync and async leaves on the asynchronous path (for example a conditional).
/// </summary>
internal enum RuleTracePhase {
    Sync = 0,
    Async = 1,
    All = 2
}

/// <summary>
/// Holds one active rule decision trace for a validation walk. The session is attached to the
/// engine-provided <see cref="RootContextData"/> so nested context validators share the same
/// boundary, and it restores the previous session when a nested validator completes.
/// </summary>
internal sealed class RuleDecisionTraceSession : IDisposable {
    private readonly IValidationDiagnosticsScope _scope;
    private readonly RootContextData? _rootData;
    private readonly RuleDecisionTraceSession? _previous;
    private bool _disposed;

    internal RuleDecisionTraceSession(IValidationDiagnosticsScope scope, RootContextData rootData) {
        _scope = scope;
        _rootData = rootData;
        _previous = rootData.RuleTrace;
        rootData.RuleTrace = this;
    }

    /// <summary>
    /// Captures the start timestamp for a rule that is about to run.
    /// </summary>
    /// <param name="session">The active session, or <see langword="null"/> when tracing is disabled.</param>
    /// <returns>The timestamp, or zero when tracing is not active.</returns>
    public static long Start(RuleDecisionTraceSession? session) =>
        session is null ? 0 : Stopwatch.GetTimestamp();

    /// <summary>
    /// Reports a rule that ran. Container rules are transparent; the loops inside the container
    /// report their leaf rules.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="rule">The rule instance.</param>
    /// <param name="context">The active validation context.</param>
    /// <param name="startedAt">The timestamp captured before the rule ran.</param>
    /// <param name="failed">Whether the rule added at least one error.</param>
    public void Complete<T>(object rule, in ValidationContext<T> context, long startedAt, bool failed) {
        if(!IsLeaf(rule)) {
            return;
        }

        Report(
            rule,
            in context,
            failed ? ValidationRuleDecision.Failed : ValidationRuleDecision.Passed,
            GetDuration(startedAt));
    }

    /// <summary>
    /// Reports every selected leaf beneath <paramref name="rule"/> with a skip or short-circuit
    /// decision.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="rule">The rule or container that did not run.</param>
    /// <param name="context">The active validation context.</param>
    /// <param name="decision">The skip or short-circuit decision.</param>
    /// <param name="phase">The phase that would have executed the rule.</param>
    public void Skip<T>(
        object rule,
        in ValidationContext<T> context,
        ValidationRuleDecision decision,
        RuleTracePhase phase) {
        foreach(var leaf in EnumerateLeaves<T>(rule, phase)) {
            Report(leaf, in context, decision, ValidationRuleDurationBucket.NotMeasured);
        }
    }

    /// <summary>
    /// Reports the remaining rules after a cascade or fail-fast stop.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <typeparam name="TRule">The rule list element type.</typeparam>
    /// <param name="rules">The selected rule list.</param>
    /// <param name="startIndex">The first rule that did not run.</param>
    /// <param name="context">The active validation context.</param>
    /// <param name="phase">The phase that was executing.</param>
    public void SkipRemaining<T, TRule>(
        IReadOnlyList<TRule> rules,
        int startIndex,
        in ValidationContext<T> context,
        RuleTracePhase phase) where TRule : class {
        for(var i = startIndex; i < rules.Count; i++) {
            Skip(rules[i], in context, ValidationRuleDecision.ShortCircuited, phase);
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        if(_disposed) {
            return;
        }

        _disposed = true;

        if(_rootData is not null && ReferenceEquals(_rootData.RuleTrace, this)) {
            _rootData.RuleTrace = _previous;
        }
    }

    private static bool IsLeaf(object rule) =>
        rule is IValidationSchemaRuleMetadata or not IValidationRuleContainer;

    private void Report<T>(
        object rule,
        in ValidationContext<T> context,
        ValidationRuleDecision decision,
        ValidationRuleDurationBucket duration) {
        var memberName = rule is IValidationMemberRule memberRule ? memberRule.MemberName : null;
        var memberPath = string.IsNullOrEmpty(memberName)
            ? context.MemberPath
            : context.GetFullPath(memberName);

        _scope.Rule(new ValidationRuleTraceEvent(GetKind(rule), memberPath, decision, duration));
    }

    private static string GetKind(object rule) => rule switch {
        IRuleMetadataKindProvider kindProvider => kindProvider.Kind,
        IValidationSchemaRuleMetadata schemaRule => schemaRule.Kind,
        IRuleMetadataProvider metadata => metadata.Kind,
        _ => ValidatorRuleSelection.GetKind(rule)
    };

    private static ValidationRuleDurationBucket GetDuration(long startedAt) {
        var milliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        return milliseconds switch {
            < 1 => ValidationRuleDurationBucket.LessThanOneMillisecond,
            < 10 => ValidationRuleDurationBucket.LessThanTenMilliseconds,
            < 100 => ValidationRuleDurationBucket.LessThanOneHundredMilliseconds,
            _ => ValidationRuleDurationBucket.OneHundredMillisecondsOrMore
        };
    }

    private static IEnumerable<object> EnumerateLeaves<T>(object rule, RuleTracePhase phase) =>
        rule switch {
            MemberRuleGroup<T> group => EnumeratePhaseLists(group.SyncRules, group.AsyncRules, phase),
            RuleSetRule<T> ruleSet => EnumeratePhaseLists(ruleSet.SyncRules, ruleSet.AsyncRules, phase),
            // The async path of a conditional runs its sync rules and its async rules.
            ConditionalRule<T> conditional => EnumeratePhaseLists(
                conditional.SyncRules,
                conditional.AsyncRules,
                phase is RuleTracePhase.Sync ? RuleTracePhase.Sync : RuleTracePhase.All),
            DependentRule<T> dependent => EnumerateDependent(dependent, phase),
            IValidationRuleContainer container when rule is not IValidationSchemaRuleMetadata =>
                EnumerateContainer<T>(container, phase),
            _ => InPhase<T>(rule, phase) ? [rule] : []
        };

    private static IEnumerable<object> EnumeratePhaseLists<T>(
        IReadOnlyList<ISyncValidationRule<T>> syncRules,
        IReadOnlyList<IAsyncValidationRule<T>> asyncRules,
        RuleTracePhase phase) {
        if(phase is RuleTracePhase.Sync or RuleTracePhase.All) {
            foreach(var rule in syncRules) {
                foreach(var leaf in EnumerateLeaves<T>(rule, phase)) {
                    yield return leaf;
                }
            }
        }

        if(phase is RuleTracePhase.Async or RuleTracePhase.All) {
            foreach(var rule in asyncRules) {
                foreach(var leaf in EnumerateLeaves<T>(rule, phase)) {
                    yield return leaf;
                }
            }
        }
    }

    private static IEnumerable<object> EnumerateDependent<T>(DependentRule<T> dependent, RuleTracePhase phase) {
        if(phase is RuleTracePhase.Sync or RuleTracePhase.All) {
            var parent = dependent.ParentSync;

            if(parent is not null) {
                foreach(var leaf in EnumerateLeaves<T>(parent, RuleTracePhase.Sync)) {
                    yield return leaf;
                }
            }

            foreach(var rule in dependent.SyncRules) {
                foreach(var leaf in EnumerateLeaves<T>(rule, RuleTracePhase.Sync)) {
                    yield return leaf;
                }
            }
        }

        if(phase is RuleTracePhase.Async or RuleTracePhase.All) {
            var asyncParent = dependent.ParentAsync;

            if(asyncParent is not null && phase is RuleTracePhase.Async) {
                foreach(var leaf in EnumerateLeaves<T>(asyncParent, RuleTracePhase.All)) {
                    yield return leaf;
                }

                foreach(var rule in dependent.SyncRules) {
                    foreach(var leaf in EnumerateLeaves<T>(rule, RuleTracePhase.All)) {
                        yield return leaf;
                    }
                }
            }

            foreach(var rule in dependent.AsyncRules) {
                foreach(var leaf in EnumerateLeaves<T>(rule, RuleTracePhase.All)) {
                    yield return leaf;
                }
            }
        }
    }

    private static IEnumerable<object> EnumerateContainer<T>(
        IValidationRuleContainer container,
        RuleTracePhase phase) {
        foreach(var inner in container.InnerRules) {
            foreach(var leaf in EnumerateLeaves<T>(inner, phase)) {
                yield return leaf;
            }
        }
    }

    private static bool InPhase<T>(object rule, RuleTracePhase phase) => phase switch {
        RuleTracePhase.Sync => rule is ISyncValidationRule<T>,
        RuleTracePhase.Async => rule is IAsyncValidationRule<T>,
        _ => rule is ISyncValidationRule<T> or IAsyncValidationRule<T>
    };
}

/// <summary>
/// Starts and reads the optional per-rule decision trace attached to a validation walk.
/// </summary>
internal static class RuleDecisionTrace {
    /// <summary>
    /// Starts a trace when the diagnostics scope opted in and a root data bag is available.
    /// </summary>
    /// <param name="scope">The current diagnostics scope, when any.</param>
    /// <param name="rootData">The engine-provided root data bag, when any.</param>
    /// <returns>The session, or <see langword="null"/> when tracing is disabled.</returns>
    public static RuleDecisionTraceSession? Begin(
        IValidationDiagnosticsScope? scope,
        RootContextData? rootData) {
        if(scope is null || rootData is null || !scope.IsRuleTraceEnabled) {
            return null;
        }

        return new RuleDecisionTraceSession(scope, rootData);
    }

    /// <summary>
    /// Gets the active trace for a context.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="context">The active validation context.</param>
    /// <returns>The active session, or <see langword="null"/> when tracing is disabled.</returns>
    public static RuleDecisionTraceSession? Current<T>(in ValidationContext<T> context) =>
        context.RootData?.RuleTrace;
}
