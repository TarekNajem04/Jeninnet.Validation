//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the member value must equal the given constant.
/// </summary>
/// <param name="value">The expected value.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class EqualAttribute(object value, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the expected value.
    /// </summary>
    public object Value { get; } = value;
}
