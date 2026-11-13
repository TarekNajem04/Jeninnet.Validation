//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the string member must start with the specified prefix.
/// </summary>
/// <param name="prefix">The required prefix.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class StartsWithAttribute(string prefix, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the required prefix.
    /// </summary>
    public string Prefix { get; } = prefix;
}
