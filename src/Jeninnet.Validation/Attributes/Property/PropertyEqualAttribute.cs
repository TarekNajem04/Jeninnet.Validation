//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the member value must equal the value of another property.
/// </summary>
/// <param name="otherProperty">The name of the other property.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class PropertyEqualAttribute(string otherProperty, string? message = null) : PropertyAttribute(otherProperty, message);
