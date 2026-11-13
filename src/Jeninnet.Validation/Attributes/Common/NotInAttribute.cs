//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes.Common;

/// <summary>
/// Specifies that a member value must not be contained within a specified set of disallowed values.
/// </summary>
/// <param name="disallowedValues">The set of disallowed values.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class NotInAttribute(object[] disallowedValues, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the set of disallowed values.
    /// </summary>
    public object[] DisallowedValues { get; } = disallowedValues;
}
