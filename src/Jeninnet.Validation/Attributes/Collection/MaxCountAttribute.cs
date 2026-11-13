//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the decorated collection member must contain at most the given number of
/// elements.
/// </summary>
/// <param name="max">The maximum number of elements.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MaxCountAttribute(int max, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the maximum number of elements.
    /// </summary>
    public int Max { get; } = max;
}
