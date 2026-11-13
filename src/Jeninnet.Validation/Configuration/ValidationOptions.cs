//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Configuration;

/// <summary>
/// Represents global configuration options for the validation engine.
/// </summary>
public sealed record ValidationOptions {
    /// <summary>
    /// Gets the default cascade mode applied to member rule groups
    /// when no explicit cascade mode is configured.
    /// </summary>
    /// <remarks>
    /// This property is an alias for <see cref="DefaultRuleLevelCascadeMode"/> retained for
    /// source compatibility. Setting either property sets the same underlying value.
    /// </remarks>
    public CascadeMode DefaultCascadeMode {
        get => DefaultRuleLevelCascadeMode;
        init => DefaultRuleLevelCascadeMode = value;
    }

    /// <summary>
    /// Gets the default rule-level cascade mode applied to member rule groups when neither the
    /// validator nor an individual rule overrides it. A per-validator
    /// <c>RuleLevelCascadeMode</c> wins over this value, and a rule-level <c>Cascade</c> call wins
    /// over both.
    /// </summary>
    public CascadeMode DefaultRuleLevelCascadeMode { get; init; } = CascadeMode.Continue;

    /// <summary>
    /// Gets the default class-level cascade mode applied to fluent validators when no explicit
    /// class-level mode is configured.
    /// </summary>
    public CascadeMode DefaultClassLevelCascadeMode { get; init; } = CascadeMode.Continue;

    /// <summary>
    /// Gets an optional global resolver for human-friendly member display names. The first
    /// argument is the validated type and the second is the code-facing member name. The resolver
    /// only affects the <c>{PropertyName}</c> message parameter; <see cref="ValidationError.MemberName"/>
    /// remains the code-facing key. A rule-level <c>WithName</c>/<c>OverridePropertyName</c> wins
    /// over the resolver.
    /// </summary>
    public Func<Type, string, string>? DisplayNameResolver { get; init; }

    /// <summary>
    /// Gets an optional instance-aware resolver for human-friendly member display names. The
    /// arguments are the validated type, the code-facing member name, and the current instance.
    /// It takes precedence over <see cref="DisplayNameResolver"/> when it returns a non-empty name.
    /// </summary>
    public Func<Type, string, object?, string>? DisplayNameResolverForInstance { get; init; }

    /// <summary>
    /// Gets an optional global resolver for the code-facing member name reported in
    /// <see cref="ValidationError.MemberName"/>. The first argument is the validated type and the
    /// second is the current member path. The resolver runs after per-rule
    /// <c>OverridePropertyName</c>/<c>WithName(string)</c> overrides and before display-name
    /// resolution; a blank result keeps the original name. It differs from
    /// <see cref="DisplayNameResolver"/>, which only changes the <c>{PropertyName}</c> message
    /// parameter.
    /// </summary>
    public Func<Type, string, string>? PropertyNameResolver { get; init; }

    /// <summary>
    /// Gets an optional global factory that creates a validator instance for a model type when the
    /// service provider has no <c>IValidator&lt;T&gt;</c> registration. The first argument is the
    /// model type, the second is the optional service provider, and the result must implement
    /// <c>IValidator&lt;T&gt;</c> for that model type.
    /// </summary>
    public Func<Type, IServiceProvider?, object?>? ValidatorFactory { get; init; }
}
