//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides a base class for defining validators using a fluent API.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="ruleLevelCascadeMode">
/// An explicit rule-level cascade mode for this validator, or <see langword="null"/> to use
/// the effective <see cref="ValidationOptions.DefaultRuleLevelCascadeMode"/> at validation time.
/// </param>
public abstract class ValidatorDefinition<T>(CascadeMode? ruleLevelCascadeMode = null) : IValidationDescriptorProvider {
    private List<ISyncValidationRule<T>> SyncRuleList { get; } = [];
    private List<IAsyncValidationRule<T>> AsyncRuleList { get; } = [];
    private List<ISanitizationStep<T>> SanitizerList { get; } = [];
    private CompositeValidator<T>? BuiltValidator { get; set; }
    private ValidationDescriptor? Descriptor { get; set; }
    [SuppressMessage(
        "Style",
        "IDE0032:Use auto property",
        Justification = "The backing field distinguishes an explicit caller value from the inherited default resolved at validation time.")]
    private CascadeMode _classLevelCascadeMode = ValidationConfiguration.Options.DefaultClassLevelCascadeMode;
    private bool _classLevelCascadeModeSet;

    internal Func<ValidationContext<T>, bool>? PreValidateHook { get; private set; }
    internal Func<ValidationContext<T>, CancellationToken, ValueTask<bool>>? PreValidateAsyncHook { get; private set; }
    internal Action<ValidationResult>? OnFailureHook { get; private set; }
    internal Func<ValidationResult, CancellationToken, ValueTask>? OnFailureAsyncHook { get; private set; }

    /// <summary>
    /// Gets or sets the class-level cascade mode for this validator. <see cref="CascadeMode.Stop"/>
    /// stops after the first member that adds errors. Rule-level cascade modes take precedence
    /// within a member, and per-call <see cref="ValidationCallOptions.CascadeMode"/> overrides this
    /// value for a single call.
    /// </summary>
    public CascadeMode ClassLevelCascadeMode {
        get => _classLevelCascadeMode;
        set {
            _classLevelCascadeMode = value;
            _classLevelCascadeModeSet = true;
        }
    }

    internal CascadeMode? ExplicitClassLevelCascadeMode =>
        _classLevelCascadeModeSet ? _classLevelCascadeMode : null;

    /// <summary>
    /// Gets the rule-level cascade mode applied to member rule groups in this validator when an
    /// individual rule does not call <c>Cascade</c>. An explicit value passed to the constructor
    /// wins; otherwise the effective <see cref="ValidationOptions.DefaultRuleLevelCascadeMode"/>
    /// is resolved at validation time. A rule-level <c>Cascade</c> call wins over this value.
    /// </summary>
    public CascadeMode RuleLevelCascadeMode { get; } =
        ruleLevelCascadeMode ?? ValidationConfiguration.Options.DefaultRuleLevelCascadeMode;

    internal CascadeMode? RuleLevelCascadeModeOverride { get; } = ruleLevelCascadeMode;

    /// <summary>
    /// Registers a hook that runs once before rule evaluation. Returning <see langword="false"/>
    /// short-circuits the validation with <see cref="ValidationResult.Success"/>; failure hooks do
    /// not run for a short-circuited call. A second registration replaces the previous hook.
    /// </summary>
    /// <param name="hook">The pre-validation callback.</param>
    /// <exception cref="ArgumentNullException"><paramref name="hook"/> is null.</exception>
    protected void PreValidate(Func<ValidationContext<T>, bool> hook) {
        ArgumentNullException.ThrowIfNull(hook);

        PreValidateHook = hook;
    }

    /// <summary>
    /// Registers an asynchronous hook that runs once before rule evaluation on the asynchronous
    /// path. Returning <see langword="false"/> short-circuits with
    /// <see cref="ValidationResult.Success"/>. Configuring this hook makes the synchronous
    /// <c>Validate</c> path throw instead of silently skipping the guard.
    /// </summary>
    /// <param name="hook">The asynchronous pre-validation callback.</param>
    /// <exception cref="ArgumentNullException"><paramref name="hook"/> is null.</exception>
    protected void PreValidateAsync(Func<ValidationContext<T>, CancellationToken, ValueTask<bool>> hook) {
        ArgumentNullException.ThrowIfNull(hook);

        PreValidateAsyncHook = hook;
    }

    /// <summary>
    /// Registers a hook that runs exactly once after rule evaluation when validation fails.
    /// A second registration replaces the previous hook.
    /// </summary>
    /// <param name="hook">The failure callback.</param>
    /// <exception cref="ArgumentNullException"><paramref name="hook"/> is null.</exception>
    protected void OnFailure(Action<ValidationResult> hook) {
        ArgumentNullException.ThrowIfNull(hook);

        OnFailureHook = hook;
    }

    /// <summary>
    /// Registers an asynchronous hook that runs exactly once after rule evaluation when validation
    /// fails on the asynchronous path. It is not invoked by the synchronous path.
    /// A second registration replaces the previous hook.
    /// </summary>
    /// <param name="hook">The asynchronous failure callback.</param>
    /// <exception cref="ArgumentNullException"><paramref name="hook"/> is null.</exception>
    protected void OnFailureAsync(Func<ValidationResult, CancellationToken, ValueTask> hook) {
        ArgumentNullException.ThrowIfNull(hook);

        OnFailureAsyncHook = hook;
    }

    /// <summary>
    /// Gets an enumerable sequence of all rules configured for this validator.
    /// </summary>
    public IEnumerable<ISyncValidationRule<T>> SyncRules => SyncRuleList;

    /// <summary>
    /// Gets an enumerable sequence of all async rules configured for this validator.
    /// </summary>
    public IEnumerable<IAsyncValidationRule<T>> AsyncRules => AsyncRuleList;

    /// <summary>
    /// Gets the descriptor for this validator. The descriptor is built once and cached.
    /// </summary>
    /// <returns>The validator descriptor.</returns>
    public IValidationDescriptor Describe() => Descriptor ??= ValidationDescriptorFactory.Create(this);

    /// <summary>
    /// Configures the validator by registering rules using the fluent API.
    /// </summary>
    /// <param name="builder">The builder that exposes rule registration methods.</param>
    protected abstract void Configure(ValidatorRuleBuilder<T> builder);

    /// <summary>
    /// Gets the sanitization steps registered for this validator. Steps run in registration
    /// order before validation rules on every <c>Validate</c>/<c>ValidateAsync</c> call.
    /// </summary>
    internal IReadOnlyList<ISanitizationStep<T>> Sanitizers => SanitizerList;

    /// <summary>
    /// Registers a sanitization step that runs before validation rules. Sanitization is
    /// preprocessing, not validation: steps are excluded from descriptors and client payloads.
    /// </summary>
    /// <param name="sanitizer">The pipeline step.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sanitizer"/> is null.</exception>
    internal void AddSanitizer(ISanitizationStep<T> sanitizer) {
        ArgumentNullException.ThrowIfNull(sanitizer);

        SanitizerList.Add(sanitizer);
    }

    /// <summary>
    /// Builds a <see cref="CompositeValidator{T}"/> instance from the configured rules.
    /// </summary>
    /// <returns>A <see cref="CompositeValidator{T}"/> that can be used to validate instances of <typeparamref name="T"/>.</returns>
    public CompositeValidator<T> Build() {
        if(BuiltValidator is not null) {
            return BuiltValidator;
        }

        var builder = new ValidatorRuleBuilder<T>(SyncRuleList, AsyncRuleList, AddSanitizer) {
            RuleLevelCascadeMode = RuleLevelCascadeMode,
            RuleLevelCascadeModeOverride = RuleLevelCascadeModeOverride
        };
        Configure(builder);
        NotifyRulesAdded();
        BuiltValidator = new CompositeValidator<T>(SyncRuleList, AsyncRuleList, GetType().FullName, SanitizerList);
        return BuiltValidator;
    }

    /// <summary>
    /// Invoked once for every leaf rule after the validator finishes configuring its rules and
    /// before the composite validator is created. Override it to adjust default messages, error
    /// codes, or severities; the hook is not part of rule execution.
    /// </summary>
    /// <param name="rule">The fully configured rule.</param>
    protected virtual void OnRuleAdded(IRuleDefinition rule) => _ = rule;

    private void NotifyRulesAdded() {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);

        foreach(var rule in EnumerateRules(SyncRuleList.Cast<object>().Concat(AsyncRuleList.Cast<object>()))) {
            if(rule is IRuleDefinition definition && seen.Add(rule)) {
                OnRuleAdded(definition);
            }
        }
    }

    private static IEnumerable<object> EnumerateRules(IEnumerable<object> rules) {
        foreach(var rule in rules) {
            if(rule is IValidationRuleContainer container) {
                foreach(var inner in EnumerateRules(container.InnerRules)) {
                    yield return inner;
                }

                continue;
            }

            yield return rule;
        }
    }
}
