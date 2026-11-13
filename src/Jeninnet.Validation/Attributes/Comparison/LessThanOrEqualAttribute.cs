//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the numeric member must be less than or equal to the given threshold.
/// </summary>
/// <param name="threshold">The threshold value.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class LessThanOrEqualAttribute(object threshold, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the threshold value.
    /// </summary>
    public object Threshold { get; } = threshold;
}
