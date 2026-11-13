//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.GeneratorModel;

/// <summary>
/// Describes one member sanitization step discovered from a <c>[Sanitize]</c> attribute.
/// Sanitization is preprocessing, not a validation rule, so it is stored separately from
/// <see cref="ValidationRuleDescriptor"/> and never reaches descriptors or client payloads.
/// </summary>
/// <param name="PropertyName">The member name.</param>
/// <param name="PropertyTypeName">The full member type name (for example <c>string?</c> or <c>System.Collections.Generic.IList&lt;string?&gt;?</c>).</param>
/// <param name="IsStringList">Whether the member is a string list (sanitized element-wise).</param>
/// <param name="Trim">Whether leading and trailing white space is removed.</param>
/// <param name="NormalizeWhitespace">Whether white-space runs are collapsed.</param>
/// <param name="RemoveControlChars">Whether control characters are removed.</param>
/// <param name="ToUpper">Whether the value is upper-cased (wins over <paramref name="ToLower"/>).</param>
/// <param name="ToLower">Whether the value is lower-cased (ignored when <paramref name="ToUpper"/> is set).</param>
/// <param name="Culture">The optional culture name used for casing; <see langword="null"/> means invariant.</param>
internal sealed record SanitizeStepModel(
    string PropertyName,
    string PropertyTypeName,
    bool IsStringList,
    bool Trim,
    bool NormalizeWhitespace,
    bool RemoveControlChars,
    bool ToUpper,
    bool ToLower,
    string? Culture
);
