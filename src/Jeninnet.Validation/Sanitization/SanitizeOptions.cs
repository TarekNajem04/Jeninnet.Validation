//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Sanitization;

/// <summary>
/// Flag-based sanitization options for a string member. Mirrors the
/// <c>[Sanitize]</c> attribute surface so attribute-driven and manually registered
/// sanitizers compose the same ordered function list.
/// </summary>
public sealed class SanitizeOptions {
    /// <summary>
    /// Gets or sets a value indicating whether leading and trailing white space is removed.
    /// </summary>
    public bool Trim { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether white-space runs are collapsed and the ends trimmed.
    /// </summary>
    public bool NormalizeWhitespace { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether control characters (except tab, line feed, and
    /// carriage return) are removed.
    /// </summary>
    public bool RemoveControlChars { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the value is upper-cased. Takes precedence over
    /// <see cref="ToLower"/> when both are set.
    /// </summary>
    public bool ToUpper { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the value is lower-cased. Ignored when
    /// <see cref="ToUpper"/> is set.
    /// </summary>
    public bool ToLower { get; set; }

    /// <summary>
    /// Gets or sets the culture used for casing. When <see langword="null"/> (the default), the
    /// invariant culture is used so results are deterministic across machines.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Builds the ordered sanitizer list: <c>Trim</c>, then <c>NormalizeWhitespace</c>, then
    /// <c>RemoveControlChars</c>, then casing.
    /// </summary>
    /// <returns>The sanitizers in application order; empty when no flag is set.</returns>
    public IReadOnlyList<Func<string?, string?>> Build() {
        var sanitizers = new List<Func<string?, string?>>(4);

        if(Trim) {
            sanitizers.Add(Sanitize.Trim);
        }

        if(NormalizeWhitespace) {
            sanitizers.Add(Sanitize.NormalizeWhitespace);
        }

        if(RemoveControlChars) {
            sanitizers.Add(Sanitize.RemoveControlChars);
        }

        var culture = Culture ?? CultureInfo.InvariantCulture;

        if(ToUpper) {
            sanitizers.Add(Sanitize.ToUpper(culture));
        } else if(ToLower) {
            sanitizers.Add(Sanitize.ToLower(culture));
        }

        return sanitizers;
    }
}
