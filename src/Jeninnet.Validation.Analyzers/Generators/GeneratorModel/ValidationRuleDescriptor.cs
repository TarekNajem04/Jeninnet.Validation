//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.GeneratorModel;

/// <summary>
/// Represents a polymorphic descriptor for a validation rule discovered by the source generator.
/// </summary>
/// <param name="Kind">The kind of rule.</param>
/// <param name="PropertyName">The name of the property the rule applies to.</param>
/// <param name="PropertyTypeName">The fully-qualified name of the property type.</param>
/// <param name="Options">Encapsulated rule options.</param>
internal abstract record ValidationRuleDescriptor(
    RuleKind Kind,
    string PropertyName,
    string PropertyTypeName,
    RuleOptions Options
) {
    /// <summary>
    /// Gets the fully-qualified name of the rule class to instantiate.
    /// </summary>
    public string RuleTypeName => Kind.GetRuleFullName();
}

/// <summary>
/// A rule descriptor with no additional parameters (e.g., <c>NotEmpty</c>).
/// </summary>
/// <param name="Kind">The kind of rule.</param>
/// <param name="PropertyName">The name of the property the rule applies to.</param>
/// <param name="PropertyTypeName">The fully-qualified name of the property type.</param>
/// <param name="Options">Encapsulated rule options.</param>
internal sealed record BasicRuleDescriptor(
    RuleKind Kind,
    string PropertyName,
    string PropertyTypeName,
    RuleOptions Options
) : ValidationRuleDescriptor(Kind, PropertyName, PropertyTypeName, Options);

/// <summary>
/// A rule descriptor with one or more parameters (e.g., <c>GreaterThan</c>, <c>Length</c>).
/// </summary>
/// <param name="Kind">The kind of rule.</param>
/// <param name="PropertyName">The name of the property the rule applies to.</param>
/// <param name="PropertyTypeName">The fully-qualified name of the property type.</param>
/// <param name="Options">Encapsulated rule options.</param>
/// <param name="Parameters">The rule parameters.</param>
internal sealed record ParametricRuleDescriptor(
    RuleKind Kind,
    string PropertyName,
    string PropertyTypeName,
    RuleOptions Options,
    IReadOnlyList<RuleParameter> Parameters
) : ValidationRuleDescriptor(Kind, PropertyName, PropertyTypeName, Options);

/// <summary>
/// A rule descriptor for nested or collection validation.
/// </summary>
/// <param name="Kind">The kind of rule.</param>
/// <param name="PropertyName">The name of the property the rule applies to.</param>
/// <param name="PropertyTypeName">The fully-qualified name of the property type.</param>
/// <param name="Options">Encapsulated rule options.</param>
/// <param name="TargetTypeName">The target type name for nested validation.</param>
internal sealed record NestedRuleDescriptor(
    RuleKind Kind,
    string PropertyName,
    string PropertyTypeName,
    RuleOptions Options,
    string TargetTypeName
) : ValidationRuleDescriptor(Kind, PropertyName, PropertyTypeName, Options);

/// <summary>
/// A rule that delegates to a static custom validator method.
/// </summary>
/// <param name="Kind">The kind of rule.</param>
/// <param name="PropertyName">The member name; empty for a model-level rule.</param>
/// <param name="PropertyTypeName">The validated value or model type.</param>
/// <param name="Options">Encapsulated rule options.</param>
/// <param name="MethodExpression">The fully qualified static method reference.</param>
/// <param name="ReturnsResult">Whether the method returns a <c>ValidationResult</c>.</param>
/// <param name="HasServiceProvider">Whether the method accepts an <c>IServiceProvider</c>.</param>
/// <param name="IsModelRule">Whether the rule targets the whole model.</param>
internal sealed record CustomRuleDescriptor(
    RuleKind Kind,
    string PropertyName,
    string PropertyTypeName,
    RuleOptions Options,
    string MethodExpression,
    bool ReturnsResult,
    bool HasServiceProvider,
    bool IsModelRule
) : ValidationRuleDescriptor(Kind, PropertyName, PropertyTypeName, Options);
