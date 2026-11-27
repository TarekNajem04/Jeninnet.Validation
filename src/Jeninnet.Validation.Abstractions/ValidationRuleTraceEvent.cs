//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Describes the outcome of one configured validation rule in an opt-in rule decision trace.
/// </summary>
public enum ValidationRuleDecision {
    /// <summary>
    /// The rule ran and added no errors.
    /// </summary>
    Passed = 0,

    /// <summary>
    /// The rule ran and added at least one error.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// A conditional rule evaluated to <see langword="false"/>; the rule did not run.
    /// </summary>
    SkippedCondition = 2,

    /// <summary>
    /// A cascade stop or fail-fast policy prevented the rule from running.
    /// </summary>
    ShortCircuited = 3
}

/// <summary>
/// Coarse duration buckets for a rule decision trace. The buckets avoid exposing exact timings
/// and keep trace payloads small.
/// </summary>
public enum ValidationRuleDurationBucket {
    /// <summary>
    /// The rule did not run, so no duration was measured.
    /// </summary>
    NotMeasured = 0,

    /// <summary>
    /// The rule completed in less than one millisecond.
    /// </summary>
    LessThanOneMillisecond = 1,

    /// <summary>
    /// The rule completed in less than ten milliseconds.
    /// </summary>
    LessThanTenMilliseconds = 2,

    /// <summary>
    /// The rule completed in less than one hundred milliseconds.
    /// </summary>
    LessThanOneHundredMilliseconds = 3,

    /// <summary>
    /// The rule completed in one hundred milliseconds or more.
    /// </summary>
    OneHundredMillisecondsOrMore = 4
}

/// <summary>
/// Describes one rule decision without carrying property values, messages, parameters, or custom
/// state. Instances are created by the core engine only when rule tracing is enabled.
/// </summary>
public readonly struct ValidationRuleTraceEvent {
    /// <summary>
    /// Initializes a rule trace event.
    /// </summary>
    /// <param name="kind">The stable rule kind (for example <c>NotEmpty</c>).</param>
    /// <param name="memberPath">The code-facing member path; empty for model-level rules.</param>
    /// <param name="decision">The rule outcome.</param>
    /// <param name="duration">The coarse duration bucket.</param>
    /// <exception cref="ArgumentException"><paramref name="kind"/> is empty or whitespace.</exception>
    public ValidationRuleTraceEvent(
        string kind,
        string memberPath,
        ValidationRuleDecision decision,
        ValidationRuleDurationBucket duration) {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        Kind = kind;
        MemberPath = memberPath ?? string.Empty;
        Decision = decision;
        Duration = duration;
    }

    /// <summary>
    /// Gets the stable rule kind (for example <c>NotEmpty</c>).
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the code-facing member path; empty for model-level rules.
    /// </summary>
    public string MemberPath { get; }

    /// <summary>
    /// Gets the rule outcome.
    /// </summary>
    public ValidationRuleDecision Decision { get; }

    /// <summary>
    /// Gets the coarse duration bucket.
    /// </summary>
    public ValidationRuleDurationBucket Duration { get; }
}
