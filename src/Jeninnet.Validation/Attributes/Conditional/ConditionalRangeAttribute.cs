//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the member must be within an inclusive range when another member equals the given value.
/// </summary>
/// <param name="otherProperty">The name of the member whose value enables the range check.</param>
/// <param name="value">The value that enables the range check. Must be a compile-time constant.</param>
/// <param name="min">The inclusive minimum value.</param>
/// <param name="max">The inclusive maximum value.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ConditionalRangeAttribute(
    string otherProperty,
    object? value,
    object min,
    object max,
    string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the name of the member whose value enables the range check.
    /// </summary>
    public string OtherProperty { get; } = otherProperty;

    /// <summary>
    /// Gets the value that enables the range check.
    /// </summary>
    public object? Value { get; } = value;

    /// <summary>
    /// Gets the inclusive minimum value.
    /// </summary>
    public object Min { get; } = min;

    /// <summary>
    /// Gets the inclusive maximum value.
    /// </summary>
    public object Max { get; } = max;
}
