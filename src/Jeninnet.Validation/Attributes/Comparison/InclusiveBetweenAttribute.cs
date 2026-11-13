//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the member value must be within the given inclusive range.
/// </summary>
/// <param name="min">The minimum value.</param>
/// <param name="max">The maximum value.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class InclusiveBetweenAttribute(object min, object max, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the minimum value.
    /// </summary>
    public object Min { get; } = min;

    /// <summary>
    /// Gets the maximum value.
    /// </summary>
    public object Max { get; } = max;
}
