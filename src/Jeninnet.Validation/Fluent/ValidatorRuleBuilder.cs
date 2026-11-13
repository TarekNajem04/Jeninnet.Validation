//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Fluent;

/// <summary>
/// Provides a fluent starting point for defining validation rules for a type.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
/// <param name="syncRules">The list of synchronous rules.</param>
/// <param name="asyncRules">The list of asynchronous rules.</param>
/// <param name="addSanitizer">Receives sanitization steps registered through this builder.</param>
public sealed class ValidatorRuleBuilder<T>(
    List<ISyncValidationRule<T>> syncRules,
    List<IAsyncValidationRule<T>> asyncRules,
    Action<ISanitizationStep<T>>? addSanitizer = null) {
    internal List<ISyncValidationRule<T>> SyncRules { get; } = syncRules;
    internal List<IAsyncValidationRule<T>> AsyncRules { get; } = asyncRules;

    /// <summary>
    /// Gets or sets the cascade mode used as the default for rule groups created by this builder.
    /// </summary>
    internal CascadeMode RuleLevelCascadeMode { get; set; } =
        ValidationConfiguration.Options.DefaultRuleLevelCascadeMode;

    internal CascadeMode? RuleLevelCascadeModeOverride { get; set; }

    /// <summary>
    /// Starts configuring validation rules for the specified member.
    /// </summary>
    /// <typeparam name="TProperty">The member type.</typeparam>
    /// <param name="getter">The function used to obtain the member value.</param>
    /// <param name="memberName">The name of the member.</param>
    /// <returns>An <see cref="IRuleBuilder{T, TProperty}"/> used to define rules.</returns>
    public IRuleBuilder<T, TProperty?> RuleFor<TProperty>(Func<T, TProperty?> getter, string memberName) {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentException.ThrowIfNullOrEmpty(memberName);

        /*
         * ========================================================================
         * [Per-member rule lists]
         * Create list Rule/AsyncRule lists for the member
         * ========================================================================
         */
        return new RuleBuilder<T, TProperty?>(
            accessor: ValidatorFactory.Accessor(getter, memberName),
            //memberRules: new List<ISyncValidationRule<T>>(),
            //memberAsyncRules: new List<IAsyncValidationRule<T>>(),
            parentBuilder: this
        );
    }

    /// <summary>
    /// Starts configuring validation rules for the specified member and records a setter so the
    /// member can be sanitized with <c>Sanitize</c>. Sanitization is in-place preprocessing that
    /// runs before validation rules; members without a setter cannot be sanitized.
    /// </summary>
    /// <typeparam name="TProperty">The member type.</typeparam>
    /// <param name="getter">The function used to obtain the member value.</param>
    /// <param name="memberName">The name of the member.</param>
    /// <param name="setter">Writes the sanitized value. A member without a setter cannot be sanitized.</param>
    /// <returns>An <see cref="IRuleBuilder{T, TProperty}"/> used to define rules and sanitizers.</returns>
    public IRuleBuilder<T, TProperty?> RuleFor<TProperty>(
        Func<T, TProperty?> getter,
        string memberName,
        Action<T, TProperty?> setter) {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentException.ThrowIfNullOrEmpty(memberName);
        ArgumentNullException.ThrowIfNull(setter);

        return new RuleBuilder<T, TProperty?>(
            accessor: ValidatorFactory.Accessor(getter, memberName),
            parentBuilder: this
        ) {
            Setter = setter
        };
    }

    /// <summary>
    /// Registers a whole-model sanitization step that runs before validation rules.
    /// </summary>
    /// <param name="sanitize">The sanitization delegate. It mutates the instance in place.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sanitize"/> is null.</exception>
    public void Sanitize(Action<T> sanitize) {
        ArgumentNullException.ThrowIfNull(sanitize);

        AddSanitizer(Sanitizers.ForModel(sanitize));
    }

    /// <summary>
    /// Registers a whole-model sanitizer that runs before validation rules.
    /// </summary>
    /// <param name="sanitizer">The sanitizer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sanitizer"/> is null.</exception>
    public void Sanitize(ISanitizer<T> sanitizer) {
        ArgumentNullException.ThrowIfNull(sanitizer);

        AddSanitizer(Sanitizers.ForModel(sanitizer));
    }

    internal void AddSanitizer(ISanitizationStep<T> sanitizer) {
        ArgumentNullException.ThrowIfNull(sanitizer);

        if(addSanitizer is null) {
            throw new InvalidOperationException(
                "Sanitize requires a validator-owned builder. Configure sanitizers inside a validator's Configure method.");
        }

        addSanitizer(sanitizer);
    }
}
