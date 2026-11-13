//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the member is required when another member equals the given value.
/// A string or collection member is checked for emptiness; any other member is checked for null.
/// </summary>
/// <param name="otherProperty">The name of the member whose value enables the requirement.</param>
/// <param name="value">The value that enables the requirement. Must be a compile-time constant.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class RequiredIfAttribute(string otherProperty, object? value, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the name of the member whose value enables the requirement.
    /// </summary>
    public string OtherProperty { get; } = otherProperty;

    /// <summary>
    /// Gets the value that enables the requirement.
    /// </summary>
    public object? Value { get; } = value;
}
