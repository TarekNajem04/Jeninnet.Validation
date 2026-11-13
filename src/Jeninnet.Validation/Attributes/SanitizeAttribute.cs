//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Attributes;

/// <summary>
/// Declares opt-in input sanitization for a string member (or, on a class, for every writable
/// string member without its own <c>[Sanitize]</c>). The source generator emits the sanitizers
/// into the generated validator so they run before validation rules; this is preprocessing, not
/// a validation rule, and never appears in descriptors or client payloads.
/// </summary>
/// <remarks>
/// Sanitization mutates the model in place and runs on every <c>Validate</c>/
/// <c>ValidateAsync</c> call once registered: there is no silent behavior because the attribute
/// itself is the explicit opt-in. Members without an accessible non-init setter are rejected at
/// build time with <c>VAL0027</c>. This is plain whitespace/casing/control-character
/// normalization, not HTML or markup sanitization; use a dedicated library for untrusted markup.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
public sealed class SanitizeAttribute : Attribute {
    /// <summary>
    /// Gets or sets a value indicating whether leading and trailing white space is removed.
    /// Applied first.
    /// </summary>
    public bool Trim { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether white-space runs are collapsed and the ends trimmed.
    /// Applied after <see cref="Trim"/>.
    /// </summary>
    public bool NormalizeWhitespace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether control characters (except tab, line feed, and
    /// carriage return) are removed. Applied after <see cref="NormalizeWhitespace"/>.
    /// </summary>
    public bool RemoveControlChars { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the value is upper-cased. Applied last and takes
    /// precedence over <see cref="ToLower"/> when both are set.
    /// </summary>
    public bool ToUpper { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the value is lower-cased. Applied last; ignored
    /// when <see cref="ToUpper"/> is set.
    /// </summary>
    public bool ToLower { get; set; }

    /// <summary>
    /// Gets or sets the culture name used for casing (for example <c>"tr-TR"</c>). When
    /// <see langword="null"/> (the default), the invariant culture is used so results are
    /// deterministic across machines. An unknown culture name fails the build with
    /// <c>VAL0027</c>.
    /// </summary>
    public string? Culture { get; set; }
}
