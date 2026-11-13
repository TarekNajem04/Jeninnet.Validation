//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Extensions;

internal static class RuleKindExtensions {
    public static string GetAttributeName(this RuleKind kind, bool removeSuffix = true) {
        var fullName = kind.GetAttributeFullName();

        if(string.IsNullOrEmpty(fullName)) {
            return string.Empty;
        }

        var parts = fullName.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        var lastPart = parts[parts.Length - 1];

        if(!removeSuffix) {
            return lastPart;
        }

        // Remove "Attribute" suffix if present
        return lastPart.EndsWith("Attribute", StringComparison.Ordinal)
            ? lastPart.Substring(0, lastPart.Length - 9)  // Remove last 9 chars ("Attribute")
            : lastPart;
    }

    /// <summary>
    /// Gets the full name of the rule associated with the specified RuleKind.
    /// </summary>
    /// <param name="kind">The RuleKind enum value.</param>
    /// <returns> The full name of the rule as a string.</returns>
    public static string GetAttributeFullName(this RuleKind kind) =>
        kind switch {
            // Collection rules
            RuleKind.ForEach => AttributeNames.FOR_EACH,
            RuleKind.Count => AttributeNames.COUNT,
            RuleKind.MinCount => AttributeNames.MIN_COUNT,
            RuleKind.MaxCount => AttributeNames.MAX_COUNT,
            RuleKind.UniqueItems => AttributeNames.UNIQUE_ITEMS,
            RuleKind.MultipleOf => AttributeNames.MULTIPLE_OF,
            // Common rules
            RuleKind.Equal => AttributeNames.EQUAL,
            RuleKind.NotEmpty => AttributeNames.NOT_EMPTY,
            RuleKind.NotEqual => AttributeNames.NOT_EQUAL,
            RuleKind.NotNull => AttributeNames.NOT_NULL,
            RuleKind.Enum => AttributeNames.ENUM,
            RuleKind.In => AttributeNames.IN,
            RuleKind.NotIn => AttributeNames.NOT_IN,

            // Comparison rules
            RuleKind.GreaterThan => AttributeNames.GREATER_THAN,
            RuleKind.GreaterThanOrEqual => AttributeNames.GREATER_THAN_OR_EQUAL,
            RuleKind.LessThan => AttributeNames.LESS_THAN,
            RuleKind.LessThanOrEqual => AttributeNames.LESS_THAN_OR_EQUAL,
            RuleKind.ExclusiveBetween => AttributeNames.EXCLUSIVE_BETWEEN,
            RuleKind.InclusiveBetween => AttributeNames.INCLUSIVE_BETWEEN,
            RuleKind.ScalePrecision => AttributeNames.SCALE_PRECISION,

            // DateTime rules
            RuleKind.After => AttributeNames.AFTER,
            RuleKind.Before => AttributeNames.BEFORE,
            RuleKind.FutureDate => AttributeNames.FUTURE_DATE,
            RuleKind.PastDate => AttributeNames.PAST_DATE,

            // Nested rules
            RuleKind.Nested => AttributeNames.NESTED,

            // String rules
            RuleKind.CreditCard => AttributeNames.CREDIT_CARD,
            RuleKind.Email => AttributeNames.EMAIL,
            RuleKind.StartsWith => AttributeNames.STARTS_WITH,
            RuleKind.EndsWith => AttributeNames.ENDS_WITH,
            RuleKind.Contains => AttributeNames.CONTAINS,
            RuleKind.Length => AttributeNames.LENGTH,
            RuleKind.MaximumLength => AttributeNames.MAXIMUM_LENGTH,
            RuleKind.MinimumLength => AttributeNames.MINIMUM_LENGTH,
            RuleKind.ExactLength => AttributeNames.EXACT_LENGTH,
            RuleKind.Base64String => AttributeNames.BASE64_STRING,
            RuleKind.FileExtensions => AttributeNames.FILE_EXTENSIONS,
            RuleKind.Regex => AttributeNames.REGEX,
            RuleKind.IPAddress => AttributeNames.IP_ADDRESS,
            RuleKind.Phone => AttributeNames.PHONE,
            RuleKind.Url => AttributeNames.URL,

            // Domain rules
            RuleKind.Iban => AttributeNames.IBAN,
            RuleKind.VatNumber => AttributeNames.VAT_NUMBER,
            RuleKind.PostalCode => AttributeNames.POSTAL_CODE,
            RuleKind.NationalId => AttributeNames.NATIONAL_ID,
            RuleKind.PasswordStrength => AttributeNames.PASSWORD_STRENGTH,
            RuleKind.Uuid => AttributeNames.UUID,
            RuleKind.Ulid => AttributeNames.ULID,

            // Property rules
            RuleKind.PropertyEqual => AttributeNames.PROPERTY_EQUAL,
            RuleKind.PropertyGreaterThanOrEqual => AttributeNames.PROPERTY_GREATER_THAN_OR_EQUAL,
            RuleKind.PropertyGreaterThan => AttributeNames.PROPERTY_GREATER_THAN,
            RuleKind.PropertyLessThanOrEqual => AttributeNames.PROPERTY_LESS_THAN_OR_EQUAL,
            RuleKind.PropertyLessThan => AttributeNames.PROPERTY_LESS_THAN,
            RuleKind.PropertyNotEqual => AttributeNames.PROPERTY_NOT_EQUAL,

            // Custom rules
            RuleKind.Custom => AttributeNames.CUSTOM_VALIDATOR,

            _ => string.Empty,
        };

    /// <summary>
    /// Gets the full name of the rule associated with the specified RuleKind.
    /// </summary>
    /// <param name="kind">The RuleKind enum value.</param>
    /// <returns> The full name of the rule as a string.</returns>
    public static string GetRuleFullName(this RuleKind kind) =>
        kind switch {
            // Collection rules
            RuleKind.ForEach => RuleNames.FOR_EACH,
            RuleKind.Count => RuleNames.COUNT,
            RuleKind.MinCount => RuleNames.MIN_COUNT,
            RuleKind.MaxCount => RuleNames.MAX_COUNT,
            RuleKind.UniqueItems => RuleNames.UNIQUE_ITEMS,
            RuleKind.MultipleOf => RuleNames.MULTIPLE_OF,

            // Common rules
            RuleKind.NotEmpty => RuleNames.NOT_EMPTY,
            RuleKind.StringNotEmpty => RuleNames.STRING_NOT_EMPTY,
            RuleKind.NotNull => RuleNames.NOT_NULL,
            RuleKind.Enum => RuleNames.ENUM,
            RuleKind.In => RuleNames.IN,
            RuleKind.NotIn => RuleNames.NOT_IN,

            // Comparison rules
            RuleKind.Equal => RuleNames.EQUAL,
            RuleKind.ExclusiveBetween => RuleNames.EXCLUSIVE_BETWEEN,
            RuleKind.GreaterThan => RuleNames.GREATER_THAN,
            RuleKind.GreaterThanOrEqual => RuleNames.GREATER_THAN_OR_EQUAL,
            RuleKind.InclusiveBetween => RuleNames.INCLUSIVE_BETWEEN,
            RuleKind.Range => RuleNames.RANGE,
            RuleKind.ScalePrecision => RuleNames.SCALE_PRECISION,
            RuleKind.LessThan => RuleNames.LESS_THAN,
            RuleKind.LessThanOrEqual => RuleNames.LESS_THAN_OR_EQUAL,
            RuleKind.NotEqual => RuleNames.NOT_EQUAL,

            // DateTime rules
            RuleKind.After => RuleNames.AFTER,
            RuleKind.Before => RuleNames.BEFORE,
            RuleKind.FutureDate => RuleNames.FUTURE_DATE,
            RuleKind.PastDate => RuleNames.PAST_DATE,

            // CollectNestedion rules
            RuleKind.Nested => RuleNames.NESTED_VALIDATOR,

            // String rules
            RuleKind.CreditCard => RuleNames.CREDIT_CARD,
            RuleKind.Email => RuleNames.EMAIL,
            RuleKind.StartsWith => RuleNames.STARTS_WITH,
            RuleKind.EndsWith => RuleNames.ENDS_WITH,
            RuleKind.Contains => RuleNames.CONTAINS,
            RuleKind.Length => RuleNames.LENGTH,
            RuleKind.MaximumLength => RuleNames.LENGTH,
            RuleKind.MinimumLength => RuleNames.LENGTH,
            RuleKind.ExactLength => RuleNames.EXACT_LENGTH,
            RuleKind.Base64String => RuleNames.BASE64_STRING,
            RuleKind.FileExtensions => RuleNames.FILE_EXTENSIONS,
            RuleKind.Regex => RuleNames.REGEX_MATCH,
            RuleKind.IPAddress => RuleNames.IP_ADDRESS,
            RuleKind.Phone => RuleNames.PHONE,
            RuleKind.Url => RuleNames.URL,

            // Domain rules
            RuleKind.Iban => RuleNames.IBAN,
            RuleKind.VatNumber => RuleNames.VAT_NUMBER,
            RuleKind.PostalCode => RuleNames.POSTAL_CODE,
            RuleKind.NationalId => RuleNames.NATIONAL_ID,
            RuleKind.PasswordStrength => RuleNames.PASSWORD_STRENGTH,
            RuleKind.Uuid => RuleNames.UUID,
            RuleKind.Ulid => RuleNames.ULID,

            // Property rules
            RuleKind.PropertyEqual => RuleNames.PROPERTY_EQUAL,
            RuleKind.PropertyGreaterThan => RuleNames.PROPERTY_GREATER_THAN,
            RuleKind.PropertyGreaterThanOrEqual => RuleNames.PROPERTY_GREATER_THAN_OR_EQUAL,
            RuleKind.PropertyLessThan => RuleNames.PROPERTY_LESS_THAN,
            RuleKind.PropertyLessThanOrEqual => RuleNames.PROPERTY_LESS_THAN_OR_EQUAL,
            RuleKind.PropertyNotEqual => RuleNames.PROPERTY_NOT_EQUAL,

            // Custom rules
            RuleKind.Custom => RuleNames.CUSTOM,
            _ => string.Empty,
        };

    public static bool IsCommonRule(this RuleKind kind) =>
        kind is RuleKind.NotEmpty or
                RuleKind.NotNull or
                RuleKind.Enum or
                RuleKind.In or
                RuleKind.NotIn;

    public static bool IsEqualityRule(this RuleKind kind) =>
        kind is RuleKind.Equal or
                RuleKind.NotEqual;

    public static bool IsComparisonRule(this RuleKind kind) =>
        kind is RuleKind.Equal or
                RuleKind.ExclusiveBetween or
                RuleKind.GreaterThan or
                RuleKind.GreaterThanOrEqual or
                RuleKind.InclusiveBetween or
                RuleKind.LessThan or
                RuleKind.LessThanOrEqual or
                RuleKind.NotEqual or
                RuleKind.ScalePrecision;

    public static bool IsPropertyComparisonRule(this RuleKind kind) =>
        kind is RuleKind.PropertyEqual or
                RuleKind.PropertyGreaterThan or
                RuleKind.PropertyGreaterThanOrEqual or
                RuleKind.PropertyLessThan or
                RuleKind.PropertyLessThanOrEqual or
                RuleKind.PropertyNotEqual;

    public static bool IsStringRule(this RuleKind kind) =>
        kind is RuleKind.CreditCard or
                RuleKind.Email or
                RuleKind.StartsWith or
                RuleKind.EndsWith or
                RuleKind.Contains or
                RuleKind.Length or
                RuleKind.MaximumLength or
                RuleKind.MinimumLength or
                RuleKind.ExactLength or
                RuleKind.Base64String or
                RuleKind.FileExtensions or
                RuleKind.Regex or
                RuleKind.IPAddress or
                RuleKind.Phone or
                RuleKind.Url or
                RuleKind.Iban or
                RuleKind.VatNumber or
                RuleKind.PostalCode or
                RuleKind.NationalId or
                RuleKind.PasswordStrength or
                RuleKind.Uuid or
                RuleKind.Ulid;
}
