//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators;

/// <summary>
/// Defines the code generation templates for validation rules.
/// </summary>
internal enum RuleTemplate {
    /// <summary>No template defined.</summary>
    None = 0,

    /// <summary>Common rules: NotEmpty, NotNull. Signature: (accessor, message).</summary>
    Common = 1,

    /// <summary>Comparison rules: Equal, NotEqual, GreaterThan, etc. Signature: (accessor, value, message).</summary>
    Comparison = 2,

    /// <summary>Range rules: InclusiveBetween, ExclusiveBetween. Signature: (accessor, min, max, message).</summary>
    Between = 3,

    /// <summary>DateTime rules: After, Before. Signature: (accessor, dateTime, message).</summary>
    DateTime = 4,

    /// <summary>Property comparison rules: PropertyEqual, etc. Signature: (accessor, otherAccessor, message).</summary>
    Property = 5,

    /// <summary>Nested validator rule. Signature: (accessor, validator).</summary>
    Nested = 6,

    /// <summary>String rules: Email, CreditCard, etc. Signature: (accessor, message).</summary>
    String = 7,

    /// <summary>Length-based rules: Length, Min/Max. Signature: (accessor, min, max, message) or (accessor, value, message).</summary>
    Length = 8,

    /// <summary>Regex rules. Signature: (accessor, pattern, message).</summary>
    Regex = 9,

    /// <summary>Collection rules. Signature: (accessor, sync, async).</summary>
    ForEach = 10,

    /// <summary>Collection count rules. Signature: (accessor, min, max, message) or (accessor, value, message).</summary>
    Count = 11,

    /// <summary>Domain rule packs. Signature depends on the rule: country, policy, or no parameter.</summary>
    Domain = 12
}

/// <summary>
/// Provides a centralized mapping from <see cref="RuleKind"/> to <see cref="RuleTemplate"/>.
/// </summary>
internal static class RuleMapping {
    /// <summary>
    /// Gets the <see cref="RuleTemplate"/> for the specified <see cref="RuleKind"/>.
    /// </summary>
    /// <param name="kind">The kind of rule.</param>
    public static RuleTemplate GetTemplate(RuleKind kind) => kind switch {
        RuleKind.ForEach => RuleTemplate.ForEach,
        RuleKind.Nested => RuleTemplate.Nested,
        RuleKind.Count or RuleKind.MinCount or RuleKind.MaxCount => RuleTemplate.Count,
        RuleKind.UniqueItems => RuleTemplate.Common,
        RuleKind.MultipleOf => RuleTemplate.Comparison,

        RuleKind.NotEmpty or RuleKind.NotNull => RuleTemplate.Common,
        RuleKind.StringNotEmpty => RuleTemplate.String,

        RuleKind.Equal or RuleKind.NotEqual or RuleKind.GreaterThan or
        RuleKind.GreaterThanOrEqual or RuleKind.LessThan or RuleKind.LessThanOrEqual => RuleTemplate.Comparison,

        RuleKind.ExclusiveBetween or RuleKind.InclusiveBetween or RuleKind.Range => RuleTemplate.Between,

        RuleKind.After or RuleKind.Before => RuleTemplate.DateTime,

        RuleKind.PropertyEqual or RuleKind.PropertyNotEqual or RuleKind.PropertyGreaterThan or
        RuleKind.PropertyGreaterThanOrEqual or RuleKind.PropertyLessThan or RuleKind.PropertyLessThanOrEqual => RuleTemplate.Property,

        RuleKind.Length or RuleKind.MaximumLength or RuleKind.MinimumLength or RuleKind.ExactLength => RuleTemplate.Length,
        RuleKind.CreditCard or RuleKind.Email or RuleKind.Base64String or RuleKind.FileExtensions => RuleTemplate.String,

        RuleKind.Iban or RuleKind.VatNumber or RuleKind.PostalCode or
        RuleKind.NationalId or RuleKind.PasswordStrength => RuleTemplate.Domain,
        RuleKind.Uuid or RuleKind.Ulid => RuleTemplate.String,

        RuleKind.Regex => RuleTemplate.Regex,

        _ => RuleTemplate.None
    };
}
