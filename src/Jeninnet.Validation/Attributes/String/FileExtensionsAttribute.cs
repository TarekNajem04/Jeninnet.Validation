//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Specifies that the decorated string member must have one of the given file extensions.
/// Extensions may be written with or without a leading dot and are matched case-insensitively by
/// default.
/// </summary>
/// <param name="extensions">The allowed file extensions (for example <c>".pdf"</c>, <c>"jpg"</c>).</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FileExtensionsAttribute(params string[] extensions) : ValidationAttribute() {
    /// <summary>
    /// Gets the allowed file extensions as configured.
    /// </summary>
    public IReadOnlyList<string> Extensions { get; } = extensions;

    /// <summary>
    /// Gets or sets a value indicating whether extension matching ignores case. Defaults to
    /// <see langword="true"/>.
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether values without an extension are accepted (for
    /// example <c>Dockerfile</c> or <c>README</c>). Defaults to <see langword="false"/>.
    /// </summary>
    public bool AllowNonStandard { get; set; }

    /// <summary>
    /// Gets or sets the optional custom validation message. This shadows the read-only base
    /// property so it can be supplied as a named attribute argument.
    /// </summary>
    public new string? Message { get; set; }
}
