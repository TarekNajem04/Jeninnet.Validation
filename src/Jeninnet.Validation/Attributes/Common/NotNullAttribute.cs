//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes.Common;

/// <summary>
/// Specifies that the member value must not be null.
/// </summary>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class NotNullAttribute(string? message = null) : ValidationAttribute(message) {
}
