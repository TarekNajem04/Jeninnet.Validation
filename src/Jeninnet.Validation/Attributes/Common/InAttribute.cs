//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes.Common;

/// <summary>
/// Specifies that a member value must be contained within a specified set of allowed values.
/// </summary>
/// <param name="allowedValues">The set of allowed values.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class InAttribute(object[] allowedValues, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the set of allowed values.
    /// </summary>
    public object[] AllowedValues { get; } = allowedValues;
}
