//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Configuration;

/// <summary>
/// Defines stable message keys for validation rules.
/// </summary>
public static class ValidationMessageKeys {
    /// <summary>Not-null validation failure message key.</summary>
    public const string NOT_NULL = "NotNull";

    /// <summary>Not-empty validation failure message key.</summary>
    public const string NOT_EMPTY = "NotEmpty";

    /// <summary>Not-default validation failure message key.</summary>
    public const string NOT_DEFAULT = "NotDefault";

    /// <summary>Enum validation failure message key.</summary>
    public const string ENUM = "Enum";

    /// <summary>Inclusion validation failure message key.</summary>
    public const string IN = "In";

    /// <summary>Exclusion validation failure message key.</summary>
    public const string NOT_IN = "NotIn";

    /// <summary>Custom predicate validation failure message key.</summary>
    public const string MUST = "Must";

    /// <summary>Async custom predicate validation failure message key.</summary>
    public const string MUST_ASYNC = "MustAsync";

    /// <summary>Between validation failure message key.</summary>
    public const string BETWEEN = "Between";

    /// <summary>Inclusive between validation failure message key.</summary>
    public const string INCLUSIVE_BETWEEN = "InclusiveBetween";

    /// <summary>Exclusive between validation failure message key.</summary>
    public const string EXCLUSIVE_BETWEEN = "ExclusiveBetween";

    /// <summary>Comparable validation failure message key.</summary>
    public const string COMPARABLE = "Comparable";

    /// <summary>Equality validation failure message key.</summary>
    public const string EQUAL = "Equal";

    /// <summary>Inequality validation failure message key.</summary>
    public const string NOT_EQUAL = "NotEqual";

    /// <summary>Greater-than validation failure message key.</summary>
    public const string GREATER_THAN = "GreaterThan";

    /// <summary>Greater-than-or-equal validation failure message key.</summary>
    public const string GREATER_THAN_OR_EQUAL = "GreaterThanOrEqual";

    /// <summary>Less-than validation failure message key.</summary>
    public const string LESS_THAN = "LessThan";

    /// <summary>Less-than-or-equal validation failure message key.</summary>
    public const string LESS_THAN_OR_EQUAL = "LessThanOrEqual";

    /// <summary>Range validation failure message key.</summary>
    public const string RANGE = "Range";

    /// <summary>Precision scale validation failure message key.</summary>
    public const string PRECISION_SCALE = "PrecisionScale";

    /// <summary>Email validation failure message key.</summary>
    public const string EMAIL = "Email";

    /// <summary>Phone validation failure message key.</summary>
    public const string PHONE = "Phone";

    /// <summary>URL validation failure message key.</summary>
    public const string URL = "Url";

    /// <summary>IP address validation failure message key.</summary>
    public const string IP_ADDRESS = "IPAddress";

    /// <summary>Credit card validation failure message key.</summary>
    public const string CREDIT_CARD = "CreditCard";

    /// <summary>Regex validation failure message key.</summary>
    public const string REGEX = "Regex";

    /// <summary>Length validation failure message key.</summary>
    public const string LENGTH = "Length";

    /// <summary>Collection count validation failure message key.</summary>
    public const string COUNT = "Count";

    /// <summary>Minimum collection count validation failure message key.</summary>
    public const string MIN_COUNT = "MinCount";

    /// <summary>Maximum collection count validation failure message key.</summary>
    public const string MAX_COUNT = "MaxCount";

    /// <summary>Unique collection items validation failure message key.</summary>
    public const string UNIQUE_ITEMS = "UniqueItems";

    /// <summary>Multiple-of validation failure message key.</summary>
    public const string MULTIPLE_OF = "MultipleOf";

    /// <summary>Exact string length validation failure message key.</summary>
    public const string EXACT_LENGTH = "ExactLength";

    /// <summary>Base64 string validation failure message key.</summary>
    public const string BASE64_STRING = "Base64String";

    /// <summary>File extension validation failure message key.</summary>
    public const string FILE_EXTENSIONS = "FileExtensions";

    /// <summary>Contains validation failure message key.</summary>
    public const string CONTAINS = "Contains";

    /// <summary>Starts-with validation failure message key.</summary>
    public const string STARTS_WITH = "StartsWith";

    /// <summary>Ends-with validation failure message key.</summary>
    public const string ENDS_WITH = "EndsWith";

    /// <summary>After-date validation failure message key.</summary>
    public const string AFTER = "After";

    /// <summary>Before-date validation failure message key.</summary>
    public const string BEFORE = "Before";

    /// <summary>Future-date validation failure message key.</summary>
    public const string FUTURE_DATE = "FutureDate";

    /// <summary>Past-date validation failure message key.</summary>
    public const string PAST_DATE = "PastDate";

    /// <summary>Invalid validation failure message key.</summary>
    public const string INVALID = "Invalid";

    /// <summary>Null validation failure message key.</summary>
    public const string NULL = "Null";

    /// <summary>Empty validation failure message key.</summary>
    public const string EMPTY = "Empty";

    /// <summary>IsEnumName validation failure message key.</summary>
    public const string IS_ENUM_NAME = "IsEnumName";

    /// <summary>IBAN validation failure message key.</summary>
    public const string IBAN = "Iban";

    /// <summary>VAT number validation failure message key.</summary>
    public const string VAT_NUMBER = "VatNumber";

    /// <summary>Postal code validation failure message key.</summary>
    public const string POSTAL_CODE = "PostalCode";

    /// <summary>National identification number validation failure message key.</summary>
    public const string NATIONAL_ID = "NationalId";

    /// <summary>Password strength validation failure message key.</summary>
    public const string PASSWORD_STRENGTH = "PasswordStrength";

    /// <summary>UUID validation failure message key.</summary>
    public const string UUID = "Uuid";

    /// <summary>ULID validation failure message key.</summary>
    public const string ULID = "Ulid";
}
