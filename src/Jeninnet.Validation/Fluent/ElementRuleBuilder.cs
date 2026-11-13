//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Collects value rules for a collection element. The element itself is the accessor target, so
/// rule errors report the element path rather than a synthetic member name.
/// </summary>
/// <typeparam name="TElement">The element type.</typeparam>
internal sealed class ElementRuleBuilder<TElement> : IRuleBuilder<TElement, TElement>, IRuleOptionsTargetBuilder, IRuleLevelCascadeModeProvider {
    private ISyncValidationRule<TElement>? _lastAddedSyncRule;
    private IAsyncValidationRule<TElement>? _lastAddedAsyncRule;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElementRuleBuilder{TElement}"/> class.
    /// </summary>
    /// <param name="cascadeMode">The inherited rule-level cascade mode, or null to inherit options.</param>
    internal ElementRuleBuilder(CascadeMode? cascadeMode = null) => CascadeMode = cascadeMode;

    /// <inheritdoc />
    public IPropertyAccessor<TElement, TElement> Accessor { get; } =
        new PropertyAccessor<TElement, TElement>(static element => element, string.Empty);

    internal List<ISyncValidationRule<TElement>> SyncRules { get; } = [];

    internal List<IAsyncValidationRule<TElement>> AsyncRules { get; } = [];

    internal CascadeMode? CascadeMode { get; private set; }

    /// <inheritdoc />
    CascadeMode? IRuleLevelCascadeModeProvider.RuleLevelCascadeMode => CascadeMode;

    /// <inheritdoc />
    public void AddRule(ISyncValidationRule<TElement> rule) {
        ArgumentNullException.ThrowIfNull(rule);

        SyncRules.Add(rule);
        _lastAddedSyncRule = rule;
        _lastAddedAsyncRule = null;
    }

    /// <inheritdoc />
    public void AddAsyncRule(IAsyncValidationRule<TElement> rule) {
        ArgumentNullException.ThrowIfNull(rule);

        AsyncRules.Add(rule);
        _lastAddedAsyncRule = rule;
        _lastAddedSyncRule = null;
    }

    /// <inheritdoc />
    public IRuleBuilder<TElement, TElement> Cascade(CascadeMode mode) {
        CascadeMode = mode;
        return this;
    }

    /// <inheritdoc />
    public IRuleBuilder<TElement, TElement> Sanitize(params Func<TElement?, TElement?>[] sanitizers) =>
        throw new InvalidOperationException(
            "Sanitize is not supported for collection elements. Sanitize the collection member itself " +
            "(for example with Sanitize.EachElement) or register a whole-model sanitizer.");

    /// <inheritdoc />
    public IRuleBuilder<TElement, TElement> Sanitize(SanitizeOptions options) =>
        throw new InvalidOperationException(
            "Sanitize is not supported for collection elements. Sanitize the collection member itself " +
            "(for example with Sanitize.EachElement) or register a whole-model sanitizer.");

    void IRuleOptionsTargetBuilder.ConfigureLastRule(RuleOptionOverrides options) {
        ArgumentNullException.ThrowIfNull(options);

        var applied = false;

        if(_lastAddedSyncRule is IRuleOptionsTarget syncTarget) {
            syncTarget.ApplyOptions(options);
            applied = true;
        }

        if(_lastAddedAsyncRule is IRuleOptionsTarget asyncTarget) {
            asyncTarget.ApplyOptions(options);
            applied = true;
        }

        if(!applied) {
            throw new InvalidOperationException(
                "Message/code/severity/state/name options must directly follow a rule that supports them.");
        }
    }
}
