//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Models;

/// <summary>
/// Represents a rule parameter.
/// </summary>
/// <param name="Name">The name of the parameter.</param>
/// <param name="Type">The type of the parameter.</param>
internal sealed record ParameterInfo(
    string? Name = null,
    string? Type = null
);

/// <summary>
/// Represents a single validation rule attached to a property,
/// as discovered from attributes during source generation.
/// </summary>
/// <param name="Kind">The kind of rule.</param>
/// <param name="PropertyName">The name of the property.</param>
/// <param name="PropertyTypeName">The type name of the property.</param>
/// <param name="Parameter1">Optional first parameter.</param>
/// <param name="Parameter2">Optional second parameter.</param>
/// <param name="Parameter3">Optional third parameter.</param>
/// <param name="Message">Optional error message.</param>
/// <param name="WhenAsyncCondition">Optional asynchronous 'when' condition.</param>
/// <param name="UnlessAsyncCondition">Optional asynchronous 'unless' condition.</param>
/// <param name="RuleSets">Optional rule sets.</param>
/// <param name="IsAsyncRule">Whether the rule is asynchronous.</param>
/// <param name="TargetTypeName">Optional target type name for nested validation.</param>
/// <param name="Severity">Optional declarative failure severity.</param>
/// <param name="ErrorCode">Optional declarative error code.</param>
/// <param name="DisplayName">Optional declarative display name.</param>
/// <param name="OverridePropertyName">Optional declarative member-name override.</param>
/// <param name="CustomState">Optional declarative static custom state.</param>
/// <param name="WhenCondition">Optional synchronous when-condition lambda expression.</param>
/// <param name="WhenConditionHasContext">Whether the when-condition receives the validation context.</param>
/// <param name="UnlessCondition">Optional synchronous unless-condition lambda expression.</param>
/// <param name="UnlessConditionHasContext">Whether the unless-condition receives the validation context.</param>
/// <param name="CustomMethodExpression">Optional fully qualified custom validator method.</param>
/// <param name="CustomReturnsResult">Whether the custom validator returns a result instead of a predicate.</param>
/// <param name="CustomHasServiceProvider">Whether the custom validator accepts an <c>IServiceProvider</c>.</param>
/// <param name="IsModelRule">Whether the rule targets the whole model rather than a member.</param>
internal sealed record RuleModel(
    RuleKind Kind,
    string PropertyName,
    string PropertyTypeName,

    ParameterInfo? Parameter1 = null,
    ParameterInfo? Parameter2 = null,
    ParameterInfo? Parameter3 = null,

    string? Message = null,
    string? WhenAsyncCondition = null,
    string? UnlessAsyncCondition = null,
    IReadOnlyList<string>? RuleSets = null,
    bool IsAsyncRule = false,
    // for nested/element types
    string? TargetTypeName = null,
    string? Severity = null,
    string? ErrorCode = null,
    string? DisplayName = null,
    string? OverridePropertyName = null,
    string? CustomState = null,
    string? WhenCondition = null,
    bool WhenConditionHasContext = false,
    string? UnlessCondition = null,
    bool UnlessConditionHasContext = false,
    string? CustomMethodExpression = null,
    bool CustomReturnsResult = false,
    bool CustomHasServiceProvider = false,
    bool IsModelRule = false
);
