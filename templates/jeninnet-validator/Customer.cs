//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace JeninnetValidationTemplate;

/// <summary>
/// A small model used by the template's validator.
/// </summary>
public sealed class Customer {
    /// <summary>
    /// Gets or sets the customer display name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the customer email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the customer age.
    /// </summary>
    public int Age { get; set; }
}
