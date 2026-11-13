//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Configuration;

/// <summary>
/// Defines the stable names used in <see cref="ValidationError.MessageParameters"/> and rule
/// metadata. Positional keys (<c>"0"</c>, <c>"1"</c>) remain available for compatibility.
/// </summary>
public static class MessageParameterNames {
    /// <summary>The display name of the member (for example, a localized label).</summary>
    public const string PropertyName = "PropertyName";

    /// <summary>The value being validated.</summary>
    public const string PropertyValue = "PropertyValue";

    /// <summary>The full member path, including nested and indexed segments.</summary>
    public const string PropertyPath = "PropertyPath";

    /// <summary>The value a comparison rule compares against.</summary>
    public const string ComparisonValue = "ComparisonValue";

    /// <summary>The other member name used by a property-to-property comparison.</summary>
    public const string ComparisonProperty = "ComparisonProperty";

    /// <summary>The inclusive or exclusive lower bound.</summary>
    public const string Min = "Min";

    /// <summary>The inclusive or exclusive upper bound.</summary>
    public const string Max = "Max";

    /// <summary>The lower bound alias used by range-style messages.</summary>
    public const string From = "From";

    /// <summary>The upper bound alias used by range-style messages.</summary>
    public const string To = "To";

    /// <summary>The actual total length of a string value.</summary>
    public const string TotalLength = "TotalLength";

    /// <summary>The required or minimum length.</summary>
    public const string Length = "Length";

    /// <summary>The exact required length.</summary>
    public const string ExactLength = "ExactLength";

    /// <summary>The regular expression pattern.</summary>
    public const string Pattern = "Pattern";

    /// <summary>The allowed file extensions.</summary>
    public const string Extensions = "Extensions";

    /// <summary>The allowed or disallowed values.</summary>
    public const string Values = "Values";

    /// <summary>The enum type name.</summary>
    public const string EnumType = "EnumType";

    /// <summary>The precision used by a scale/precision rule.</summary>
    public const string Precision = "Precision";

    /// <summary>The scale used by a scale/precision rule.</summary>
    public const string Scale = "Scale";

    /// <summary>The divisor used by a multiple-of rule.</summary>
    public const string Divisor = "Divisor";

    /// <summary>The actual number of collection items.</summary>
    public const string Count = "Count";

    /// <summary>The zero-based index of an element inside a collection.</summary>
    public const string CollectionIndex = "CollectionIndex";

    /// <summary>The substring, prefix, or suffix used by a string containment rule.</summary>
    public const string Value = "Value";

    /// <summary>The explicit country code used by a domain rule.</summary>
    public const string Country = "Country";

    /// <summary>The named password policy used by a password-strength rule.</summary>
    public const string Policy = "Policy";

    /// <summary>The minimum length required by a password policy.</summary>
    public const string MinLength = "MinLength";

    /// <summary>The password requirements that were not satisfied.</summary>
    public const string MissingRequirements = "MissingRequirements";
}
