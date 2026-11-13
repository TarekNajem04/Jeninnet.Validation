//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the decorated string member must satisfy a password policy preset.
/// </summary>
/// <param name="preset">The password policy preset.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PasswordStrengthAttribute(
    PasswordStrengthPreset preset = PasswordStrengthPreset.Strong,
    string? message = null)
    : ValidationAttribute(message) {
    /// <summary>
    /// Gets the password policy preset.
    /// </summary>
    public PasswordStrengthPreset Preset { get; } = preset;
}
