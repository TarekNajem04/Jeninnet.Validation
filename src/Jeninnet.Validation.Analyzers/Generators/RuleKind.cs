//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators;

/// <summary>
/// Represents the logical kind of validation rule discovered from attributes.
/// </summary>
internal enum RuleKind {
    #region Common
    NotEmpty = 0,
    StringNotEmpty = 1,
    NotEqual = 2,
    NotNull = 3,
    Enum = 4,
    In = 5,
    NotIn = 6,
    #endregion Common

    #region Comparison
    Equal = 7,
    ExclusiveBetween = 8,
    GreaterThan = 9,
    GreaterThanOrEqual = 10,
    InclusiveBetween = 11,
    Range = 12,
    ScalePrecision = 13,
    LessThan = 14,
    LessThanOrEqual = 15,
    #endregion Comparison

    #region DateTime
    After = 16,
    Before = 17,
    #endregion DateTime

    #region Property comparison
    PropertyEqual = 18,
    PropertyGreaterThan = 19,
    PropertyGreaterThanOrEqual = 20,
    PropertyLessThan = 21,
    PropertyLessThanOrEqual = 22,
    PropertyNotEqual = 23,
    #endregion Property comparison

    #region String
    CreditCard = 24,
    Email = 25,
    StartsWith = 26,
    EndsWith = 27,
    Contains = 28,
    Length = 29,
    MaximumLength = 30,
    MinimumLength = 31,
    Regex = 32,
    IPAddress = 33,
    Phone = 34,
    Url = 35,
    ExactLength = 41,
    Base64String = 42,
    FileExtensions = 43,
    #endregion String

    #region Nested / collections
    ForEach = 36,
    Nested = 37,
    Count = 38,
    MinCount = 39,
    MaxCount = 40,
    #endregion Nested / collections

    #region Custom
    Custom = 44,
    #endregion Custom

    #region Domain
    Iban = 45,
    VatNumber = 46,
    PostalCode = 47,
    NationalId = 48,
    PasswordStrength = 49,
    Uuid = 50,
    Ulid = 51,
    FutureDate = 52,
    PastDate = 53,
    #endregion Domain

    #region Collection value rules
    UniqueItems = 54,
    #endregion Collection value rules

    #region Numeric value rules
    MultipleOf = 55,
    #endregion Numeric value rules
}
