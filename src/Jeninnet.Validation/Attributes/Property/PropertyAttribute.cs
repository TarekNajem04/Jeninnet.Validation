//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Base class for property-to-property comparison attributes.
/// </summary>
/// <param name="otherProperty">The name of the other property to compare against.</param>
/// <param name="message">Optional custom validation message.</param>
public abstract class PropertyAttribute(string otherProperty, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the name of the other property to compare against.
    /// </summary>
    public string OtherProperty { get; } = otherProperty;
}
