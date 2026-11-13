//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Represents a group of validation rules associated with a specific member, executed according to a configured cascade mode.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public sealed class MemberRuleGroup<T> : ISyncValidationRule<T>, IAsyncValidationRule<T>, IValidationMemberRule, IValidationRuleContainer {
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberRuleGroup{T}"/> class. The rule
    /// sequences are copied, so later mutations of the source sequences do not change the group.
    /// </summary>
    /// <param name="memberName">The name of the member the rules are associated with.</param>
    /// <param name="cascadeMode">
    /// The explicit cascade mode for this group, or <see langword="null"/> to inherit the
    /// effective <see cref="ValidationOptions.DefaultRuleLevelCascadeMode"/> at validation time.
    /// </param>
    /// <param name="syncRules">The synchronous rules for the member.</param>
    /// <param name="asyncRules">The asynchronous rules for the member.</param>
    public MemberRuleGroup(
        string memberName,
        CascadeMode? cascadeMode = null,
        IEnumerable<ISyncValidationRule<T>>? syncRules = null,
        IEnumerable<IAsyncValidationRule<T>>? asyncRules = null
    ) {
        ArgumentException.ThrowIfNullOrEmpty(memberName);

        MemberName = memberName;
        CascadeMode = cascadeMode;
        MutableSyncRules = syncRules is null ? [] : [.. syncRules];
        MutableAsyncRules = asyncRules is null ? [] : [.. asyncRules];
        SyncRules = MutableSyncRules.AsReadOnly();
        AsyncRules = MutableAsyncRules.AsReadOnly();
    }

    /// <summary>
    /// Gets the name of the member the rules are associated with.
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// Gets or sets the cascade mode controlling rule execution. <see langword="null"/> inherits
    /// the effective <see cref="ValidationOptions.DefaultRuleLevelCascadeMode"/> at validation time.
    /// </summary>
    public CascadeMode? CascadeMode { get; internal set; }

    /// <summary>
    /// Gets the synchronous rules for the member. The returned collection is a read-only view;
    /// rules are added through the fluent builder before the validator is built.
    /// </summary>
    public IReadOnlyList<ISyncValidationRule<T>> SyncRules { get; }

    /// <summary>
    /// Gets the asynchronous rules for the member. The returned collection is a read-only view;
    /// rules are added through the fluent builder before the validator is built.
    /// </summary>
    public IReadOnlyList<IAsyncValidationRule<T>> AsyncRules { get; }

    /// <summary>
    /// Gets the mutable synchronous rules used by the fluent builder during configuration.
    /// </summary>
    internal List<ISyncValidationRule<T>> MutableSyncRules { get; }

    /// <summary>
    /// Gets the mutable asynchronous rules used by the fluent builder during configuration.
    /// </summary>
    internal List<IAsyncValidationRule<T>> MutableAsyncRules { get; }

    IEnumerable<object> IValidationRuleContainer.InnerRules {
        get {
            foreach(var rule in SyncRules) {
                yield return rule;
            }

            foreach(var rule in AsyncRules) {
                yield return rule;
            }
        }
    }

    bool IValidationRuleContainer.HasSyncContent => RuleContentInspector.HasSyncContent(SyncRules);

    IReadOnlyDictionary<string, object?>? IValidationRuleContainer.Parameters => null;

    bool IValidationRuleContainer.HasCondition => false;

    string? IValidationRuleContainer.MemberName => MemberName;

    /// <inheritdoc />
    public void Validate(in ValidationContext<T> context, ValidationErrorCollector errors) {
        ArgumentNullException.ThrowIfNull(errors);
        if(SyncRules.Count is 0) {
            return;
        }

        // We don't dive into Child Context here yet because 'rules' are ISyncValidationRule<T> (targeting the parent object).
        // Instead, the INDIVIDUAL rules must use the context to build the path.
        var startCount = errors.Count;
        var effectiveCascadeMode = ResolveCascadeMode(in context);

        for(var i = 0; i < SyncRules.Count; i++) {
            var rule = SyncRules[i];
            rule.Validate(in context, errors);

            if((effectiveCascadeMode is Jeninnet.Validation.CascadeMode.Stop || context.FailFast) && errors.Count > startCount) {
                break;
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask ValidateAsync(
        ValidationContext<T> context,
        ValidationErrorCollector errors,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(errors);
        if(AsyncRules.Count is 0) {
            return;
        }

        // We don't dive into Child Context here yet because 'rules' are ISyncValidationRule<T> (targeting the parent object).
        // Instead, the INDIVIDUAL rules must use the context to build the path.
        var startCount = errors.Count;
        var effectiveCascadeMode = ResolveCascadeMode(in context);

        for(var i = 0; i < AsyncRules.Count; i++) {
            var rule = AsyncRules[i];
            await rule.ValidateAsync(context, errors, cancellationToken).ConfigureAwait(false);

            if((effectiveCascadeMode is Jeninnet.Validation.CascadeMode.Stop || context.FailFast) && errors.Count > startCount) {
                break;
            }
        }
    }

    private CascadeMode ResolveCascadeMode(in ValidationContext<T> context) =>
        CascadeMode ??
        context.Options?.DefaultRuleLevelCascadeMode ??
        ValidationConfiguration.Options.DefaultRuleLevelCascadeMode;
}
