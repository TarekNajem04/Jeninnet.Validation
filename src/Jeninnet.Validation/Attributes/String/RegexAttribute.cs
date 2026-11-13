//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the string member must match the given regular expression pattern.
/// </summary>
/// <param name="pattern">The regular expression pattern.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class RegexAttribute(string pattern, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the regular expression pattern.
    /// </summary>
    public string Pattern { get; } = pattern;
}
