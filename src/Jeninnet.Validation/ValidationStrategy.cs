//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Chained per-call strategy builder: include/exclude members and rule sets, override the
/// class-level cascade mode, and enable fail-fast. Used through
/// <c>Validator&lt;T&gt;.Validate(instance, strategy =&gt; ...)</c>.
/// </summary>
public sealed class ValidationStrategy {
    private const string DEFAULT_RULE_SET_NAME = "default";
    private readonly List<string> _includeProperties = [];
    private readonly List<string> _excludeProperties = [];
    private readonly List<string> _excludeRuleSets = [];
    private readonly List<string> _ruleSets = [];
    private CascadeMode? _cascadeModeOverride;
    private bool _failFast;
    private bool _includeAllRuleSets;
    private bool _throwOnFailures;
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Includes only the listed member names for this call (ordinal matching).
    /// </summary>
    /// <param name="properties">The member names to include.</param>
    /// <returns>The same strategy for chaining.</returns>
    public ValidationStrategy IncludeProperties(params string[] properties) {
        AddRange(_includeProperties, properties);
        return this;
    }

    /// <summary>
    /// Excludes the listed member names for this call (ordinal matching).
    /// </summary>
    /// <param name="properties">The member names to exclude.</param>
    /// <returns>The same strategy for chaining.</returns>
    public ValidationStrategy ExcludeProperties(params string[] properties) {
        AddRange(_excludeProperties, properties);
        return this;
    }

    /// <summary>
    /// Selects the listed rule sets for this call (case-insensitive).
    /// </summary>
    /// <param name="ruleSets">The rule-set names to include.</param>
    /// <returns>The same strategy for chaining.</returns>
    public ValidationStrategy IncludeRuleSets(params string[] ruleSets) {
        AddRange(_ruleSets, ruleSets);
        return this;
    }

    /// <summary>
    /// Selects every named rule set for this call. Exclusions still apply.
    /// </summary>
    /// <returns>The same strategy for chaining.</returns>
    public ValidationStrategy IncludeAllRuleSets() {
        _includeAllRuleSets = true;
        return this;
    }

    /// <summary>
    /// Selects only rules that are not part of a named rule set. This is the explicit form of
    /// <c>IncludeRuleSets("default")</c>.
    /// </summary>
    /// <returns>The same strategy for chaining.</returns>
    public ValidationStrategy IncludeRulesNotInRuleSet() => IncludeRuleSets(DEFAULT_RULE_SET_NAME);

    /// <summary>
    /// Excludes the listed rule sets for this call (case-insensitive). Use
    /// <c>"default"</c> to exclude ungrouped rules.
    /// </summary>
    /// <param name="ruleSets">The rule-set names to exclude.</param>
    /// <returns>The same strategy for chaining.</returns>
    public ValidationStrategy ExcludeRuleSets(params string[] ruleSets) {
        AddRange(_excludeRuleSets, ruleSets);
        return this;
    }

    /// <summary>
    /// Overrides the validator's class-level cascade mode for this call.
    /// </summary>
    /// <param name="cascadeMode">The class-level cascade mode.</param>
    /// <returns>The same strategy for chaining.</returns>
    public ValidationStrategy WithCascadeMode(CascadeMode cascadeMode) {
        _cascadeModeOverride = cascadeMode;
        return this;
    }

    /// <summary>
    /// Stops validation at the first error for this call.
    /// </summary>
    /// <param name="failFast">Whether fail-fast is enabled (default <see langword="true"/>).</param>
    /// <returns>The same strategy for chaining.</returns>
    public ValidationStrategy FailFast(bool failFast = true) {
        _failFast = failFast;
        return this;
    }

    /// <summary>
    /// Throws <see cref="ValidationException"/> when validation fails instead of returning the
    /// failed <see cref="ValidationResult"/>. The exception contains every collected error.
    /// </summary>
    /// <param name="throwOnFailures">Whether failures throw (default <see langword="true"/>).</param>
    /// <returns>The same strategy for chaining.</returns>
    public ValidationStrategy ThrowOnFailures(bool throwOnFailures = true) {
        _throwOnFailures = throwOnFailures;
        return this;
    }

    /// <summary>
    /// Sets the service provider exposed to rules for this call. Integration packages resolve
    /// services themselves; the engine never does.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The same strategy for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is null.</exception>
    public ValidationStrategy WithServiceProvider(IServiceProvider serviceProvider) {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _serviceProvider = serviceProvider;
        return this;
    }

    internal ValidationCallOptions Build() => new() {
        RuleSets = _ruleSets.Count > 0 ? _ruleSets : null,
        IncludeAllRuleSets = _includeAllRuleSets,
        ThrowOnFailures = _throwOnFailures,
        CascadeMode = _cascadeModeOverride,
        FailFast = _failFast,
        IncludeProperties = _includeProperties.Count > 0 ? _includeProperties : null,
        ExcludeProperties = _excludeProperties.Count > 0 ? _excludeProperties : null,
        ExcludeRuleSets = _excludeRuleSets.Count > 0 ? _excludeRuleSets : null,
        ServiceProvider = _serviceProvider
    };

    private static void AddRange(List<string> target, string[] values) {
        ArgumentNullException.ThrowIfNull(values);

        foreach(var value in values) {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            target.Add(value);
        }
    }
}
