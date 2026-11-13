//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the string member must have a length within the given inclusive range.
/// </summary>
/// <param name="min">The minimum length.</param>
/// <param name="max">The maximum length.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class LengthAttribute(int min, int max, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the minimum length.
    /// </summary>
    public int Min { get; } = min;

    /// <summary>
    /// Gets the maximum length.
    /// </summary>
    public int Max { get; } = max;
}
