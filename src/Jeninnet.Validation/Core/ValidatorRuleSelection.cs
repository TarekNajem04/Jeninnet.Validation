//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Core;

/// <summary>
/// Bridges runtime rules to the public selector contract and caches descriptors per rule instance.
/// </summary>
internal static class ValidatorRuleSelection {
    private static readonly ConditionalWeakTable<object, ValidationRuleDescriptor> _descriptors = [];
    private static readonly ConcurrentDictionary<Type, string> _kinds = new();

    /// <summary>
    /// Gets the configured selector from the current call, if any.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="context">The validation context.</param>
    /// <returns>The selector, or <see langword="null"/> when selection is disabled.</returns>
    public static IValidatorSelector? GetSelector<T>(in ValidationContext<T> context) =>
        context.RootData?.Filter.Selector;

    /// <summary>
    /// Determines whether a rule should execute under the configured selector.
    /// </summary>
    /// <typeparam name="T">The validated model type.</typeparam>
    /// <param name="rule">The rule instance.</param>
    /// <param name="memberName">The full member path for the rule.</param>
    /// <param name="context">The validation context.</param>
    /// <returns><see langword="true"/> when no selector is configured or the selector accepts the rule.</returns>
    public static bool CanExecute<T>(object rule, string memberName, in ValidationContext<T> context) {
        var selector = GetSelector(in context);

        return selector?.CanExecute(GetDescriptor(rule), memberName, context) != false;
    }

    /// <summary>
    /// Gets a cached descriptor for a runtime rule.
    /// </summary>
    /// <param name="rule">The rule instance.</param>
    /// <returns>The immutable descriptor.</returns>
    public static ValidationRuleDescriptor GetDescriptor(object rule) {
        ArgumentNullException.ThrowIfNull(rule);

        return _descriptors.GetValue(rule, static instance => CreateDescriptor(instance));
    }

    private static ValidationRuleDescriptor CreateDescriptor(object rule) {
        var kind = rule is IRuleMetadataKindProvider kindProvider
            ? kindProvider.Kind
            : _kinds.GetOrAdd(rule.GetType(), static type => RuleMetadataKind.FromType(type));

        if(rule is IRuleMetadataProvider metadata) {
            return new ValidationRuleDescriptor(
                kind,
                metadata.MessageKey,
                metadata.Severity,
                metadata.HasCondition,
                metadata.GetMetadataParameters(),
                metadata.ErrorCode,
                metadata.DisplayName,
                metadata.CustomState) {
                MessageIsAuthored = metadata.MessageIsAuthored
            };
        }

        return new ValidationRuleDescriptor(kind);
    }
}
