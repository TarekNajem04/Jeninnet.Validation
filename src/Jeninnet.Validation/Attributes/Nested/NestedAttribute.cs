//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the decorated property should be validated using its own validator.
/// The type of the property must also be annotated with <see cref="ValidateAttribute"/>.
/// </summary>
/// <param name="typename">Indicates the optional typename of the validator to use for nested validation.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class NestedAttribute(string? typename = null, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// The optional typename of the validator to use for nested validation.
    /// </summary>
    /// <remarks>
    /// If it is not provided, the system will attempt to determine the appropriate validator based on the type of property that will be generated via Auto Generators.
    /// </remarks>
    public string? Typename { get; set; } = typename;
}
