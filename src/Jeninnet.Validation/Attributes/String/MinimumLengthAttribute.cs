//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the string member must have at least the given minimum length.
/// </summary>
/// <param name="min">The minimum length.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MinimumLengthAttribute(int min, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the minimum length.
    /// </summary>
    public int Min { get; } = min;
}
