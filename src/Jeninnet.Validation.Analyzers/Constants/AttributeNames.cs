//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Constants;

/// <summary>
/// Provides the simple and fully qualified names for validation attributes
/// used by the source generator.
/// </summary>
internal static class AttributeNames {
    private const string BASE = NamespaceNames.Attributes;

    #region Full Names
    // Base

    public const string VALIDATION_ATTRIBUTE = $"{BASE}.ValidationAttribute";

    // Class-level

    public const string VALIDATE = $"{BASE}.ValidateAttribute";

    // Collection

    public const string FOR_EACH = $"{BASE}.ForEachAttribute";
    public const string COUNT = $"{BASE}.CountAttribute";
    public const string MIN_COUNT = $"{BASE}.MinCountAttribute";
    public const string MAX_COUNT = $"{BASE}.MaxCountAttribute";
    public const string UNIQUE_ITEMS = $"{BASE}.UniqueItemsAttribute";
    public const string MULTIPLE_OF = $"{BASE}.MultipleOfAttribute";
    public const string EXACT_LENGTH = $"{BASE}.ExactLengthAttribute";
    public const string BASE64_STRING = $"{BASE}.Base64StringAttribute";
    public const string FILE_EXTENSIONS = $"{BASE}.FileExtensionsAttribute";

    // Common rules

    public const string NOT_EMPTY = $"{BASE}.NotEmptyAttribute";
    public const string NOT_NULL = $"{BASE}.NotNullAttribute";
    public const string ENUM = $"{BASE}.Common.EnumAttribute";
    public const string IN = $"{BASE}.Common.InAttribute";
    public const string NOT_IN = $"{BASE}.Common.NotInAttribute";

    // Comparison rules

    public const string EQUAL = $"{BASE}.EqualAttribute";
    public const string EXCLUSIVE_BETWEEN = $"{BASE}.ExclusiveBetweenAttribute";
    public const string GREATER_THAN_OR_EQUAL = $"{BASE}.GreaterThanOrEqualAttribute";
    public const string GREATER_THAN = $"{BASE}.GreaterThanAttribute";
    public const string INCLUSIVE_BETWEEN = $"{BASE}.InclusiveBetweenAttribute";
    public const string LESS_THAN_OR_EQUAL = $"{BASE}.LessThanOrEqualAttribute";
    public const string LESS_THAN = $"{BASE}.LessThanAttribute";
    public const string NOT_EQUAL = $"{BASE}.NotEqualAttribute";
    public const string SCALE_PRECISION = $"{BASE}.Common.ScalePrecisionAttribute";

    // Conditional

    public const string UNLESS_ASYNC_CONDITION = $"{BASE}.UnlessAsyncConditionAttribute";
    public const string WHEN_ASYNC_CONDITION = $"{BASE}.WhenAsyncConditionAttribute";
    public const string WHEN_CONDITION = $"{BASE}.WhenConditionAttribute";
    public const string UNLESS_CONDITION = $"{BASE}.UnlessConditionAttribute";
    public const string REQUIRED_IF = $"{BASE}.RequiredIfAttribute";
    public const string REQUIRED_UNLESS = $"{BASE}.RequiredUnlessAttribute";
    public const string CONDITIONAL_RANGE = $"{BASE}.ConditionalRangeAttribute";
    public const string CUSTOM_VALIDATOR = $"{BASE}.CustomValidatorAttribute";
    public const string PRE_VALIDATE = $"{BASE}.PreValidateAttribute";
    public const string ON_FAILURE = $"{BASE}.OnFailureAttribute";

    // DateTime

    public const string AFTER = $"{BASE}.AfterAttribute";
    public const string BEFORE = $"{BASE}.BeforeAttribute";
    public const string FUTURE_DATE = $"{BASE}.FutureDateAttribute";
    public const string PAST_DATE = $"{BASE}.PastDateAttribute";

    // Nested

    public const string NESTED = $"{BASE}.NestedAttribute";

    // Numeric rules

    // Property comparison

    public const string PROPERTY_EQUAL = $"{BASE}.PropertyEqualAttribute";
    public const string PROPERTY_GREATER_THAN = $"{BASE}.PropertyGreaterThanAttribute";
    public const string PROPERTY_GREATER_THAN_OR_EQUAL = $"{BASE}.PropertyGreaterThanOrEqualAttribute";
    public const string PROPERTY_LESS_THAN = $"{BASE}.PropertyLessThanAttribute";
    public const string PROPERTY_LESS_THAN_OR_EQUAL = $"{BASE}.PropertyLessThanOrEqualAttribute";
    public const string PROPERTY_NOT_EQUAL = $"{BASE}.PropertyNotEqualAttribute";

    // String rules

    public const string CREDIT_CARD = $"{BASE}.CreditCardAttribute";
    public const string EMAIL = $"{BASE}.EmailAttribute";
    public const string STARTS_WITH = $"{BASE}.StartsWithAttribute";
    public const string ENDS_WITH = $"{BASE}.EndsWithAttribute";
    public const string CONTAINS = $"{BASE}.ContainsAttribute";
    public const string LENGTH = $"{BASE}.LengthAttribute";
    public const string MAXIMUM_LENGTH = $"{BASE}.MaximumLengthAttribute";
    public const string MINIMUM_LENGTH = $"{BASE}.MinimumLengthAttribute";
    public const string REGEX = $"{BASE}.RegexAttribute";
    public const string IP_ADDRESS = $"{BASE}.String.IPAddressAttribute";
    public const string PHONE = $"{BASE}.String.PhoneAttribute";
    public const string URL = $"{BASE}.String.UrlAttribute";

    // Domain rules

    public const string IBAN = $"{BASE}.IbanAttribute";
    public const string VAT_NUMBER = $"{BASE}.VatNumberAttribute";
    public const string POSTAL_CODE = $"{BASE}.PostalCodeAttribute";
    public const string NATIONAL_ID = $"{BASE}.NationalIdAttribute";
    public const string PASSWORD_STRENGTH = $"{BASE}.PasswordStrengthAttribute";
    public const string UUID = $"{BASE}.UuidAttribute";
    public const string ULID = $"{BASE}.UlidAttribute";

    // Set rules

    public const string RULE_SET = $"{BASE}.RuleSetAttribute";

    // Sanitization (preprocessing, not a rule)

    public const string SANITIZE = $"{BASE}.SanitizeAttribute";

    // Validation opt-out

    public const string SKIP_VALIDATION = $"{BASE}.SkipValidationAttribute";
    #endregion Full Names

    #region Simple Names (without Attribute suffix)
    public const string SIMPLE_VALIDATE = "Validate";
    public const string SIMPLE_FOR_EACH = "ForEach";
    public const string SIMPLE_COUNT = "Count";
    public const string SIMPLE_MIN_COUNT = "MinCount";
    public const string SIMPLE_MAX_COUNT = "MaxCount";
    public const string SIMPLE_UNIQUE_ITEMS = "UniqueItems";
    public const string SIMPLE_MULTIPLE_OF = "MultipleOf";
    public const string SIMPLE_EXACT_LENGTH = "ExactLength";
    public const string SIMPLE_BASE64_STRING = "Base64String";
    public const string SIMPLE_FILE_EXTENSIONS = "FileExtensions";
    public const string SIMPLE_NOT_EMPTY = "NotEmpty";
    public const string SIMPLE_NOT_NULL = "NotNull";
    public const string SIMPLE_ENUM = "Enum";
    public const string SIMPLE_IN = "In";
    public const string SIMPLE_NOT_IN = "NotIn";
    public const string SIMPLE_EQUAL = "Equal";
    public const string SIMPLE_EXCLUSIVE_BETWEEN = "ExclusiveBetween";
    public const string SIMPLE_GREATER_THAN_OR_EQUAL = "GreaterThanOrEqual";
    public const string SIMPLE_GREATER_THAN = "GreaterThan";
    public const string SIMPLE_INCLUSIVE_BETWEEN = "InclusiveBetween";
    public const string SIMPLE_LESS_THAN_OR_EQUAL = "LessThanOrEqual";
    public const string SIMPLE_LESS_THAN = "LessThan";
    public const string SIMPLE_NOT_EQUAL = "NotEqual";
    public const string SIMPLE_SCALE_PRECISION = "ScalePrecision";
    public const string SIMPLE_AFTER = "After";
    public const string SIMPLE_BEFORE = "Before";
    public const string SIMPLE_NESTED = "Nested";
    public const string SIMPLE_PROPERTY_EQUAL = "PropertyEqual";
    public const string SIMPLE_PROPERTY_GREATER_THAN = "PropertyGreaterThan";
    public const string SIMPLE_PROPERTY_GREATER_THAN_OR_EQUAL = "PropertyGreaterThanOrEqual";
    public const string SIMPLE_PROPERTY_LESS_THAN = "PropertyLessThan";
    public const string SIMPLE_PROPERTY_LESS_THAN_OR_EQUAL = "PropertyLessThanOrEqual";
    public const string SIMPLE_PROPERTY_NOT_EQUAL = "PropertyNotEqual";
    public const string SIMPLE_CREDIT_CARD = "CreditCard";
    public const string SIMPLE_EMAIL = "Email";
    public const string SIMPLE_STARTS_WITH = "StartsWith";
    public const string SIMPLE_ENDS_WITH = "EndsWith";
    public const string SIMPLE_CONTAINS = "Contains";
    public const string SIMPLE_LENGTH = "Length";
    public const string SIMPLE_MAXIMUM_LENGTH = "MaximumLength";
    public const string SIMPLE_MINIMUM_LENGTH = "MinimumLength";
    public const string SIMPLE_REGEX = "Regex";
    public const string SIMPLE_IP_ADDRESS = "IPAddress";
    public const string SIMPLE_PHONE = "Phone";
    public const string SIMPLE_URL = "Url";
    public const string SIMPLE_RULE_SET = "RuleSet";
    public const string SIMPLE_SANITIZE = "Sanitize";
    #endregion Simple Names (without Attribute suffix)
}
