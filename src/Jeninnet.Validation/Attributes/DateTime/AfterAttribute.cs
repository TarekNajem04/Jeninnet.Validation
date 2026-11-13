//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the date/time member must be after the given value.
/// </summary>
/// <param name="value">The date/time value as a string.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class AfterAttribute(string value, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the date/time value as a string.
    /// </summary>
    public string Value { get; } = value;
}
