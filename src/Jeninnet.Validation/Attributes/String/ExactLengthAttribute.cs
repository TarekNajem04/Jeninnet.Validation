//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the decorated string member must have exactly the given length.
/// </summary>
/// <param name="length">The required length.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ExactLengthAttribute(int length, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the required length.
    /// </summary>
    public int Length { get; } = length;
}
