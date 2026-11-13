//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Per-call validation options: rule-set selection, class-level cascade override, and fail-fast.
/// </summary>
public sealed record ValidationCallOptions {
    /// <summary>
    /// Gets the rule sets to apply. When <see langword="null"/> or empty, all rules run, including
    /// rules in named sets. Pass <c>["default"]</c> to run only ungrouped rules, or set
    /// <see cref="IncludeAllRuleSets"/> to select every set explicitly.
    /// </summary>
    public IEnumerable<string>? RuleSets { get; init; }

    /// <summary>
    /// Gets a value indicating whether every named rule set is selected for this call. Explicit
    /// <see cref="RuleSets"/> selection is bypassed; <see cref="ExcludeRuleSets"/> still applies.
    /// </summary>
    public bool IncludeAllRuleSets { get; init; }

    /// <summary>
    /// Gets a value indicating whether a failed validation throws <see cref="ValidationException"/>
    /// instead of returning the failed result. The exception contains all collected errors.
    /// </summary>
    public bool ThrowOnFailures { get; init; }

    /// <summary>
    /// Gets the class-level cascade mode for this call, overriding the validator's
    /// <c>ClassLevelCascadeMode</c>. Rule-level cascade modes still take precedence within a member.
    /// </summary>
    public CascadeMode? CascadeMode { get; init; }

    /// <summary>
    /// Gets a value indicating whether validation stops at the first error, total. When
    /// <see langword="true"/>, rule groups stop internally at their first failure and the remaining
    /// members (including the async phase after a sync failure) are skipped.
    /// </summary>
    public bool FailFast { get; init; }

    /// <summary>
    /// Gets the member names to include. When non-null, only fluent member groups whose
    /// <c>MemberName</c> is listed are evaluated (flat generated rules are excluded); an empty
    /// collection evaluates nothing. Matching is ordinal.
    /// </summary>
    public IReadOnlyCollection<string>? IncludeProperties { get; init; }

    /// <summary>
    /// Gets the member names to exclude. Exclusion wins over inclusion. Matching is ordinal.
    /// </summary>
    public IReadOnlyCollection<string>? ExcludeProperties { get; init; }

    /// <summary>
    /// Gets the rule-set names to exclude (case-insensitive). The special name
    /// <c>"default"</c> excludes rules that are not part of a named rule set.
    /// </summary>
    public IReadOnlyCollection<string>? ExcludeRuleSets { get; init; }

    /// <summary>
    /// Gets the optional service provider exposed to rules through
    /// <see cref="ValidationContext{T}.ServiceProvider"/>. The engine never resolves services itself;
    /// integration packages use this hook.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; init; }

    /// <summary>
    /// Gets the optional custom selector applied after rule-set and include/exclude filtering.
    /// A rule executes only when the selector returns <see langword="true"/>.
    /// </summary>
    public IValidatorSelector? Selector { get; init; }
}
