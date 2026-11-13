//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Base class for all validation attributes.
/// </summary>
/// <param name="message">The custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public abstract class ValidationAttribute(string? message = null) : Attribute {
    /// <summary>
    /// Gets the optional custom validation message.
    /// </summary>
    public string? Message { get; } = message;

    /// <summary>
    /// Gets or sets the failure severity. The default is
    /// <see cref="ValidationErrorSeverity.Error"/>.
    /// </summary>
    public ValidationErrorSeverity Severity { get; set; } = ValidationErrorSeverity.Error;

    /// <summary>
    /// Gets or sets the machine-readable error code.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the display name used for the <c>{PropertyName}</c> message parameter.
    /// This does not change <see cref="ValidationError.MemberName"/>.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets a member name that overrides the code-facing
    /// <see cref="ValidationError.MemberName"/>.
    /// </summary>
    public string? OverridePropertyName { get; set; }

    /// <summary>
    /// Gets or sets a static custom state value. Attribute arguments must be compile-time
    /// constants, so the declarative path supports strings only; use the fluent API for arbitrary
    /// objects.
    /// </summary>
    public string? CustomState { get; set; }

    /// <summary>
    /// Gets or sets a rule-set name for this rule only. It overrides a property-level
    /// <c>[RuleSet]</c> and a class-level <c>[RuleSet]</c> for the decorated rule.
    /// </summary>
    public string? RuleSet { get; set; }
}
