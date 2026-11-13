//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the decorated string member must be a valid IBAN.
/// </summary>
/// <param name="country">The optional required IBAN country code.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class IbanAttribute(string? country = null, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the optional required IBAN country code.
    /// </summary>
    public string? Country { get; } = country;
}
