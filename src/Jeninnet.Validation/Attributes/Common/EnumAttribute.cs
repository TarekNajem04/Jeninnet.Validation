//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes.Common;

/// <summary>
/// Specifies that a member must be a valid enum value.
/// </summary>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class EnumAttribute(string? message = null) : ValidationAttribute(message) {
}
