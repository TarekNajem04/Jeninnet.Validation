//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Constants;

internal static class RuleNames {
    private const string BASE = NamespaceNames.Rules;

    // Collection rules

    public const string FOR_EACH = $"{BASE}.ForEachRule";
    public const string COUNT = $"{BASE}.CountRule";
    public const string MIN_COUNT = $"{BASE}.MinCountRule";
    public const string MAX_COUNT = $"{BASE}.MaxCountRule";
    public const string UNIQUE_ITEMS = $"{BASE}.UniqueItemsRule";
    public const string MULTIPLE_OF = $"{BASE}.MultipleOfRule";
    public const string EXACT_LENGTH = $"{BASE}.ExactLengthRule";
    public const string BASE64_STRING = $"{BASE}.Base64StringRule";
    public const string FILE_EXTENSIONS = $"{BASE}.FileExtensionsRule";

    // Common rules

    public const string MUST = $"{BASE}.MustRule";
    public const string MUST_ASYNC_CONDITION = $"{BASE}.MustAsyncCondition";
    public const string NOT_EMPTY = $"{BASE}.NotEmptyRule";
    public const string STRING_NOT_EMPTY = $"{BASE}.StringNotEmptyRule";
    public const string NOT_NULL = $"{BASE}.NotNullRule";
    public const string ENUM = $"{BASE}.EnumRule";
    public const string IN = $"{BASE}.InRule";
    public const string NOT_IN = $"{BASE}.NotInRule";
    public const string RANGE = $"{BASE}.RangeRule";

    // Comparison rules

    public const string EQUAL = $"{BASE}.EqualRule";
    public const string EXCLUSIVE_BETWEEN = $"{BASE}.ExclusiveBetweenRule";
    public const string GREATER_THAN_OR_EQUAL = $"{BASE}.GreaterThanOrEqualRule";
    public const string GREATER_THAN = $"{BASE}.GreaterThanRule";
    public const string INCLUSIVE_BETWEEN = $"{BASE}.InclusiveBetweenRule";
    public const string LESS_THAN_OR_EQUAL = $"{BASE}.LessThanOrEqualRule";
    public const string LESS_THAN = $"{BASE}.LessThanRule";
    public const string NOT_EQUAL = $"{BASE}.NotEqualRule";
    public const string SCALE_PRECISION = $"{BASE}.ScalePrecisionRule";

    // Conditional rules

    public const string CONDITIONAL = $"{BASE}.ConditionalRule";

    // Custom rules

    public const string CUSTOM = $"{BASE}.CustomRule";
    public const string CUSTOM_ASYNC = $"{BASE}.CustomAsyncRule";

    // DateTime

    public const string AFTER = $"{BASE}.AfterRule";
    public const string BEFORE = $"{BASE}.BeforeRule";
    public const string FUTURE_DATE = $"{BASE}.FutureDateRule";
    public const string PAST_DATE = $"{BASE}.PastDateRule";

    // Nested rules

    public const string NESTED_VALIDATOR = $"{BASE}.NestedValidatorRule";

    // Property comparison rules

    public const string PROPERTY_EQUAL = $"{BASE}.PropertyEqualRule";
    public const string PROPERTY_GREATER_THAN_OR_EQUAL = $"{BASE}.PropertyGreaterThanOrEqualRule";
    public const string PROPERTY_GREATER_THAN = $"{BASE}.PropertyGreaterThanRule";
    public const string PROPERTY_LESS_THAN_OR_EQUAL = $"{BASE}.PropertyLessThanOrEqualRule";
    public const string PROPERTY_LESS_THAN = $"{BASE}.PropertyLessThanRule";
    public const string PROPERTY_NOT_EQUAL = $"{BASE}.PropertyNotEqualRule";

    // Set rules

    public const string RULE_SET = $"{BASE}.RuleSetRule";

    // String rules

    public const string CREDIT_CARD = $"{BASE}.CreditCardRule";
    public const string EMAIL = $"{BASE}.EmailRule";
    public const string STARTS_WITH = $"{BASE}.StartsWithRule";
    public const string ENDS_WITH = $"{BASE}.EndsWithRule";
    public const string CONTAINS = $"{BASE}.ContainsRule";
    public const string LENGTH = $"{BASE}.LengthRule";
    public const string REGEX_MATCH = $"{BASE}.RegexMatchRule";
    public const string IP_ADDRESS = $"{BASE}.IPAddressRule";
    public const string PHONE = $"{BASE}.PhoneRule";
    public const string URL = $"{BASE}.UrlRule";

    // Domain rules

    public const string IBAN = $"{BASE}.IbanRule";
    public const string VAT_NUMBER = $"{BASE}.VatNumberRule";
    public const string POSTAL_CODE = $"{BASE}.PostalCodeRule";
    public const string NATIONAL_ID = $"{BASE}.NationalIdRule";
    public const string PASSWORD_STRENGTH = $"{BASE}.PasswordStrengthRule";
    public const string UUID = $"{BASE}.UuidRule";
    public const string ULID = $"{BASE}.UlidRule";
}
