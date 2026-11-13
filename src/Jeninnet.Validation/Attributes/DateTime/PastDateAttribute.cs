//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the date/time member must be in the past relative to the current UTC time.
/// </summary>
/// <remarks>
/// The current time comes from <see cref="TimeProvider"/> resolved through the validation
/// context's service provider, falling back to <see cref="TimeProvider.System"/>. Comparisons use
/// UTC; no local-time conversion is performed. A <see langword="null"/> value passes; use
/// <c>[NotNull]</c> when presence is also required.
/// </remarks>
/// <param name="message">Optional custom validation message.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PastDateAttribute(string? message = null) : ValidationAttribute(message) {
    /// <summary>
    /// Gets or sets a value indicating whether today's UTC date counts as valid. The default is
    /// <see langword="false"/>, which requires a strictly earlier instant.
    /// </summary>
    public bool IncludeToday { get; set; }
}
