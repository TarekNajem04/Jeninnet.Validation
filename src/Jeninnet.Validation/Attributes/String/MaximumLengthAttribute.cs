//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the string member must not exceed the given maximum length.
/// </summary>
/// <param name="max">The maximum length.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MaximumLengthAttribute(int max, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the maximum length.
    /// </summary>
    public int Max { get; } = max;
}
