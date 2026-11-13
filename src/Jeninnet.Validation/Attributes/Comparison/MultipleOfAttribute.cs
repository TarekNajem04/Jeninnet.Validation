//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the decorated numeric member must be a multiple of the given divisor.
/// </summary>
/// <param name="divisor">The positive divisor.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MultipleOfAttribute(object divisor, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the divisor.
    /// </summary>
    public object Divisor { get; } = divisor;
}
