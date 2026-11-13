//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the string member must end with the specified suffix.
/// </summary>
/// <param name="suffix">The required suffix.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class EndsWithAttribute(string suffix, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the required suffix.
    /// </summary>
    public string Suffix { get; } = suffix;
}
