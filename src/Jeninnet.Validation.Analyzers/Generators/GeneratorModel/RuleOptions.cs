//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.GeneratorModel;

/// <summary>
/// Encapsulates execution-time options for a validation rule.
/// </summary>
/// <param name="Message">Optional custom validation message.</param>
/// <param name="WhenAsyncCondition">Optional name of an async condition method (must return true).</param>
/// <param name="UnlessAsyncCondition">Optional name of an async condition method (must return false).</param>
/// <param name="RuleSets">Optional list of rule sets the rule belongs to.</param>
/// <param name="IsAsyncRule">Indicates whether the rule is asynchronous.</param>
/// <param name="Severity">Optional declarative failure severity.</param>
/// <param name="ErrorCode">Optional declarative error code.</param>
/// <param name="DisplayName">Optional declarative display name.</param>
/// <param name="OverridePropertyName">Optional declarative member-name override.</param>
/// <param name="CustomState">Optional declarative static custom state.</param>
/// <param name="WhenCondition">Optional synchronous when-condition lambda expression.</param>
/// <param name="WhenConditionHasContext">Whether the when-condition receives the validation context.</param>
/// <param name="UnlessCondition">Optional synchronous unless-condition lambda expression.</param>
/// <param name="UnlessConditionHasContext">Whether the unless-condition receives the validation context.</param>
internal sealed record RuleOptions(
    string? Message = null,
    string? WhenAsyncCondition = null,
    string? UnlessAsyncCondition = null,
    IReadOnlyList<string>? RuleSets = null,
    bool IsAsyncRule = false,
    string? Severity = null,
    string? ErrorCode = null,
    string? DisplayName = null,
    string? OverridePropertyName = null,
    string? CustomState = null,
    string? WhenCondition = null,
    bool WhenConditionHasContext = false,
    string? UnlessCondition = null,
    bool UnlessConditionHasContext = false
);
