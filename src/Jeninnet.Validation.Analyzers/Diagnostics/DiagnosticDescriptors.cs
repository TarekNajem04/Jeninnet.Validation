//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Diagnostics;

/// <summary>
/// Contains all diagnostic descriptors used by the Jeninnet.Validation analyzers and source generator.
/// </summary>
public static class DiagnosticDescriptors {
    private const string CATEGORY_VALIDATION_GENERATOR = "ValidationGenerator";

    /// <summary>Reported when a rule parameter value cannot be parsed to the expected type.</summary>
    public static readonly DiagnosticDescriptor InvalidRuleParameter = new(
        id: "VAL0001",
        title: "Invalid rule parameter",
        messageFormat: "The value \"{0}\" cannot be parsed as type \"{1}\" for rule \"{2}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a validation rule is applied to a property type it cannot validate.</summary>
    public static readonly DiagnosticDescriptor RuleAppliedToIncompatibleType = new(
        id: "VAL0002",
        title: "Rule applied to incompatible property type",
        messageFormat: "Rule \"{0}\" cannot be applied to property \"{1}\" of type \"{2}\" {3}",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a rule parameter's type does not match the expected type.</summary>
    public static readonly DiagnosticDescriptor RuleParameterTypeMismatch = new(
        id: "VAL0003",
        title: "Rule parameter type mismatch",
        messageFormat: "Rule \"{0}\" expects a parameter of type \"{1}\", but received \"{2}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when no validator is found for a nested type referenced by a [Validate] or [ForEach] attribute.</summary>
    public static readonly DiagnosticDescriptor NestedValidatorNotFound = new(
        id: "VAL0004",
        title: "Nested validator not found",
        messageFormat: "No validator was found for nested type \"{0}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>Reported when no validator is found for a collection element type referenced by a [ForEach] attribute.</summary>
    public static readonly DiagnosticDescriptor CollectionElementValidatorNotFound = new(
        id: "VAL0005",
        title: "Collection element validator not found",
        messageFormat: "No validator was found for collection element type \"{0}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a rule that requires a non-nullable type is applied to a nullable property.</summary>
    public static readonly DiagnosticDescriptor RuleNotAllowedOnNullableType = new(
        id: "VAL0006",
        title: "Rule cannot be used on nullable type",
        messageFormat: "Rule \"{0}\" cannot be applied to nullable type \"{1}\" on the property \"{2}\". Consider adjusting the rule \"{3}\" or using a non-nullable type.",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a property has multiple validation rules that conflict with each other.</summary>
    public static readonly DiagnosticDescriptor ConflictingRules = new(
        id: "VAL0007",
        title: "Conflicting rules on property",
        messageFormat: "Property \"{0}\" has multiple conflicting rules: {1}",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when an attribute argument has a type that is not supported for the rule.</summary>
    public static readonly DiagnosticDescriptor UnsupportedAttributeArgumentType = new(
        id: "VAL0008",
        title: "Unsupported attribute argument type",
        messageFormat: "Attribute argument \"{0}\" is not supported for rule \"{1}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when two or more rules on the same property cannot be combined.</summary>
    public static readonly DiagnosticDescriptor InvalidRuleCombination = new(
        id: "VAL0009",
        title: "Invalid rule combination",
        messageFormat: "Rule \"{0}\" cannot be combined with rule \"{1}\" on the same property \"{2}\" {3}",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a rule parameter value is outside the valid range for its type.</summary>
    public static readonly DiagnosticDescriptor RuleParameterOutOfRange = new(
        id: "VAL0010",
        title: "Rule parameter out of range",
        messageFormat: "Parameter \"{0}\" for rule \"{1}\" is outside the valid range for type \"{2}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>Suggested when a string property participates in validation but has no [NotEmpty] attribute.</summary>
    public static readonly DiagnosticDescriptor SuggestNotEmptyForString = new(
        id: "VAL0011",
        title: "Consider adding [NotEmpty] for string property",
        messageFormat: "Property \"{0}\" is a string and participates in validation; consider adding [NotEmpty]",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );

    /// <summary>Suggested when a nullable property has validation rules but no [NotNull] attribute.</summary>
    public static readonly DiagnosticDescriptor SuggestNotNullForNullable = new(
        id: "VAL0012",
        title: "Consider adding [NotNull] for nullable property",
        messageFormat: "Property \"{0}\" is nullable and has validation rules; consider adding [NotNull]",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );
    /// <summary>Reported when a validation pattern can be simplified to a more concise rule combination.</summary>
    public static readonly DiagnosticDescriptor SimplifyRuls = new(
        id: "VAL0013",
        title: "Rule can be simplified",
        messageFormat: "The process of validating the property \"{0}\" can be simplified by using \"{1}\" instead of \"{2}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a [NotNull] attribute is applied to a non-nullable property where it has no effect.</summary>
    public static readonly DiagnosticDescriptor RedundantNotNull = new(
        id: "VAL0014",
        title: "Redundant [NotNull] attribute",
        messageFormat: "The property \"{0}\" is non-nullable; [NotNull] is redundant and can be removed",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a property-to-property comparison rule references a target property that does not exist.</summary>
    public static readonly DiagnosticDescriptor TargetPropertyNotFound = new(
        id: "VAL0015",
        title: "Target property not found",
        messageFormat: "Rule \"{0}\" references a property \"{1}\" that does not exist on type \"{2}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when the synchronous Validate API is invoked on a validator whose rules are all asynchronous.</summary>
    public static readonly DiagnosticDescriptor SyncValidateOnAsyncOnlyValidator = new(
        id: "VAL0016",
        title: "Synchronous Validate on async-only validator",
        messageFormat: "\"{0}\" is marked [AsyncOnlyValidator]; synchronous Validate() throws at runtime, use ValidateAsync() instead",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a rule's bounds cover the entire value range, making the rule a tautology.</summary>
    public static readonly DiagnosticDescriptor TautologicalRule = new(
        id: "VAL0017",
        title: "Tautological validation rule",
        messageFormat: "The rule \"{0}\" on member \"{1}\" is always satisfied and can be removed",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a synchronous condition method is missing or has an invalid signature.</summary>
    public static readonly DiagnosticDescriptor InvalidConditionMethod = new(
        id: "VAL0018",
        title: "Invalid condition method",
        messageFormat: "Condition method \"{0}\" on type \"{1}\" must be an accessible instance or static method matching {2}",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a lifecycle hook method is missing or has an invalid signature.</summary>
    public static readonly DiagnosticDescriptor InvalidLifecycleHook = new(
        id: "VAL0019",
        title: "Invalid lifecycle hook",
        messageFormat: "Lifecycle hook \"{0}\" on type \"{1}\" must be {2}",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a custom validator method is missing or has an invalid signature.</summary>
    public static readonly DiagnosticDescriptor InvalidCustomValidator = new(
        id: "VAL0020",
        title: "Invalid custom validator",
        messageFormat: "Custom validator method \"{0}\" on type \"{1}\" must be {2}",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a date/time rule value cannot be parsed as the property type.</summary>
    public static readonly DiagnosticDescriptor InvalidDateTimeValue = new(
        id: "VAL0021",
        title: "Invalid date/time rule value",
        messageFormat: "The value \"{0}\" cannot be parsed as type \"{1}\" for the date/time rule on \"{2}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a generic model cannot be validated by the source generator.</summary>
    public static readonly DiagnosticDescriptor UnsupportedGenericModel = new(
        id: "VAL0022",
        title: "Generic model is not supported",
        messageFormat: "Generic type \"{0}\" cannot be validated by the source generator; use a non-generic model or a fluent validator",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when the member name passed to RuleFor does not match the selected member.</summary>
    public static readonly DiagnosticDescriptor RuleForMemberNameMismatch = new(
        id: "VAL0023",
        title: "RuleFor member name mismatch",
        messageFormat: "RuleFor selects member \"{1}\" but passes member name \"{0}\"; use nameof({2}.{1}) or \"{1}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>Reported when Count, MinCount, or MaxCount is applied to a non-collection target.</summary>
    public static readonly DiagnosticDescriptor CollectionRuleRequiresCollection = new(
        id: "VAL0024",
        title: "Collection rule requires a collection",
        messageFormat: "Rule \"{0}\" requires a collection, but the target type is \"{1}\"",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>Reported when an async-only validator is used by the synchronous DataAnnotations adapter.</summary>
    public static readonly DiagnosticDescriptor AsyncOnlyValidatorInSyncAdapter = new(
        id: "VAL0025",
        title: "Async-only validator in synchronous adapter",
        messageFormat: "Validator \"{0}\" is marked [AsyncOnlyValidator] but \"{1}\" calls the synchronous DataAnnotations path; use an async integration path or add a synchronous rule",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>Reported when [Sanitize] cannot be applied to a member.</summary>
    public static readonly DiagnosticDescriptor SanitizeNotApplicable = new(
        id: "VAL0027",
        title: "Sanitize cannot be applied",
        messageFormat: "[Sanitize] cannot be applied to \"{0}\": {1}",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when [SkipValidation] is combined with validation attributes on the same property.</summary>
    public static readonly DiagnosticDescriptor SkipValidationConflict = new(
        id: "VAL0028",
        title: "SkipValidation conflicts with validation attributes",
        messageFormat: "[SkipValidation] on \"{0}\" is combined with validation attributes; remove the attributes or the skip marker",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a conditional attribute references an invalid member.</summary>
    public static readonly DiagnosticDescriptor ConditionalTargetInvalid = new(
        id: "VAL0029",
        title: "Conditional attribute has an invalid target",
        messageFormat: "The conditional attribute \"{0}\" on \"{1}\" has an invalid target: {2}",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a conditional attribute value cannot be compared with the target member.</summary>
    public static readonly DiagnosticDescriptor ConditionalValueUnsupported = new(
        id: "VAL0030",
        title: "Conditional attribute value is not supported",
        messageFormat: "The conditional attribute \"{0}\" on \"{1}\" has an unsupported value: {2}",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when the same conditional attribute targets a member more than once.</summary>
    public static readonly DiagnosticDescriptor ConditionalAttributeDuplicate = new(
        id: "VAL0031",
        title: "Conditional attribute is duplicated",
        messageFormat: "The conditional attribute \"{0}\" on \"{1}\" targets \"{2}\" more than once; combine the conditions or remove the duplicate",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a relative-date attribute is applied to an unsupported member type.</summary>
    public static readonly DiagnosticDescriptor RelativeDateUnsupportedType = new(
        id: "VAL0032",
        title: "Relative date rule requires a date/time member",
        messageFormat: "[{0}] on \"{1}\" requires DateTime, DateTimeOffset, or DateOnly; \"{2}\" is not supported",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when an exclusive range has equal bounds and can never match.</summary>
    public static readonly DiagnosticDescriptor DegenerateExclusiveRange = new(
        id: "VAL0033",
        title: "Exclusive range has equal bounds",
        messageFormat: "[{0}] on \"{1}\" has equal bounds ({2}); an exclusive range can never match. Widen the bounds or use an inclusive range.",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>Reported when a multiple-of divisor is not a positive finite number.</summary>
    public static readonly DiagnosticDescriptor InvalidMultipleOfDivisor = new(
        id: "VAL0034",
        title: "Invalid multiple-of divisor",
        messageFormat: "The divisor \"{0}\" for [MultipleOf] on \"{1}\" must be a positive finite number",
        category: CATEGORY_VALIDATION_GENERATOR,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
