//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Metadata;

/// <summary>
/// Describes one configured validation rule without exposing its executable predicate.
/// </summary>
public interface IValidationRuleDescriptor {
    /// <summary>
    /// Gets the stable rule kind (for example <c>NotEmpty</c>, <c>Email</c>, or
    /// <c>GreaterThan</c>).
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Gets the message key used to localize the failure, when one is configured.
    /// </summary>
    string? MessageKey { get; }

    /// <summary>
    /// Gets a value indicating whether the rule carries an explicitly authored message that must
    /// take precedence over localization packs.
    /// </summary>
    bool MessageIsAuthored => false;

    /// <summary>
    /// Gets the severity of the failure.
    /// </summary>
    ValidationErrorSeverity Severity { get; }

    /// <summary>
    /// Gets the optional machine-readable error code.
    /// </summary>
    string? ErrorCode { get; }

    /// <summary>
    /// Gets the optional display name for the <c>{PropertyName}</c> parameter.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Gets the optional static custom state.
    /// </summary>
    object? CustomState { get; }

    /// <summary>
    /// Gets a value indicating whether the rule is gated by a per-rule condition.
    /// </summary>
    bool HasCondition { get; }

    /// <summary>
    /// Gets the rule parameters keyed by name or positional index.
    /// </summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }
}

/// <summary>
/// Implemented by composite rules that can expose a nested descriptor to schema writers.
/// </summary>
public interface IValidationSchemaRuleMetadata {
    /// <summary>
    /// Gets the stable rule kind.
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Gets a value indicating whether the rule applies to every element of a collection.
    /// </summary>
    bool IsCollection { get; }

    /// <summary>
    /// Gets the nested validator descriptor, when the child validator exposes one.
    /// </summary>
    IValidationDescriptor? NestedDescriptor { get; }
}

/// <summary>
/// Describes the rules configured for one member.
/// </summary>
public interface IValidationMemberDescriptor {
    /// <summary>
    /// Gets the member name (the code-facing name, not a localized display name).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the rules configured for the member.
    /// </summary>
    IReadOnlyList<IValidationRuleDescriptor> Rules { get; }
}

/// <summary>
/// Describes a validator's members and rules.
/// </summary>
public interface IValidationDescriptor {
    /// <summary>
    /// Gets the validator type name.
    /// </summary>
    string ValidatorName { get; }

    /// <summary>
    /// Gets the validated model type, when known.
    /// </summary>
    Type? ValidatedType { get; }

    /// <summary>
    /// Gets all member descriptors.
    /// </summary>
    IReadOnlyList<IValidationMemberDescriptor> Members { get; }

    /// <summary>
    /// Gets the rules configured for a member. Returns an empty list when the member is unknown.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <returns>The member's rules, or an empty list.</returns>
    IReadOnlyList<IValidationRuleDescriptor> RulesForMember(string memberName);
}

/// <summary>
/// Implemented by validators that can expose their rule metadata.
/// </summary>
public interface IValidationDescriptorProvider {
    /// <summary>
    /// Gets the validator descriptor. Implementations should return a frozen or cached instance.
    /// </summary>
    /// <returns>The validator descriptor.</returns>
    IValidationDescriptor Describe();
}
