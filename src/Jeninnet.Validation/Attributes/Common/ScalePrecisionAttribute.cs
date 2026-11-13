//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes.Common;

/// <summary>
/// Specifies that a decimal member must have a precision and scale within the specified limits.
/// </summary>
/// <param name="precision">The maximum number of digits.</param>
/// <param name="scale">The number of digits to the right of the decimal point.</param>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ScalePrecisionAttribute(int precision, int scale, string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets the maximum number of digits.
    /// </summary>
    public int Precision { get; } = precision;

    /// <summary>
    /// Gets the number of digits to the right of the decimal point.
    /// </summary>
    public int Scale { get; } = scale;

    /// <summary>
    /// Gets or sets a value indicating whether trailing fractional zeros are ignored when
    /// counting the scale.
    /// </summary>
    public bool IgnoreTrailingZeros { get; set; }
}
