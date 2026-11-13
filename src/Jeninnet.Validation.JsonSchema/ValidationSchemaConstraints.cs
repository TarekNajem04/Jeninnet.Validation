//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.JsonSchema;

/// <summary>
/// The schema constraints inferred for one member.
/// </summary>
public sealed class ValidationSchemaConstraints {
    /// <summary>
    /// Gets or sets the JSON type (<c>string</c>, <c>number</c>, or <c>array</c>).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the minimum string length.
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Gets or sets the maximum string length.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the regular-expression pattern.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the inclusive minimum.
    /// </summary>
    public decimal? Minimum { get; set; }

    /// <summary>
    /// Gets or sets the inclusive maximum.
    /// </summary>
    public decimal? Maximum { get; set; }

    /// <summary>
    /// Gets or sets the exclusive minimum.
    /// </summary>
    public decimal? ExclusiveMinimum { get; set; }

    /// <summary>
    /// Gets or sets the exclusive maximum.
    /// </summary>
    public decimal? ExclusiveMaximum { get; set; }

    /// <summary>
    /// Gets or sets the string format.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets alternative string formats for rules such as <c>IPAddress</c>, which accepts
    /// both IPv4 and IPv6.
    /// </summary>
    public IReadOnlyList<string>? AnyOfFormats { get; set; }

    /// <summary>
    /// Gets or sets the content encoding (for example <c>base64</c>).
    /// </summary>
    public string? ContentEncoding { get; set; }

    /// <summary>
    /// Gets or sets the minimum array length.
    /// </summary>
    public int? MinItems { get; set; }

    /// <summary>
    /// Gets or sets the maximum array length.
    /// </summary>
    public int? MaxItems { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the array elements must be unique.
    /// </summary>
    public bool? UniqueItems { get; set; }

    /// <summary>
    /// Gets or sets the allowed values.
    /// </summary>
    public IReadOnlyList<string>? Enum { get; set; }

    /// <summary>
    /// Gets or sets the CLR enum type name for an <c>Enum</c> rule.
    /// </summary>
    public string? EnumType { get; set; }

    /// <summary>
    /// Gets or sets the numeric multiple.
    /// </summary>
    public decimal? MultipleOf { get; set; }

    /// <summary>
    /// Gets or sets the allowed file extensions.
    /// </summary>
    public IReadOnlyList<string>? FileExtensions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the member is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the declared member type, when known.
    /// </summary>
    public Type? MemberType { get; set; }

    /// <summary>
    /// Gets or sets the nested descriptor for a <c>Nested</c> rule.
    /// </summary>
    public IValidationDescriptor? NestedDescriptor { get; set; }

    /// <summary>
    /// Gets or sets the element descriptor for a <c>ForEach</c> rule.
    /// </summary>
    public IValidationDescriptor? ItemDescriptor { get; set; }

    /// <summary>
    /// Gets or sets the rules that have no direct schema mapping.
    /// </summary>
    public IReadOnlyList<IValidationRuleDescriptor> AdvisoryRules { get; set; } = [];
}

/// <summary>
/// The mapped schema for a validator.
/// </summary>
public sealed class ValidationObjectSchema {
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationObjectSchema"/> class.
    /// </summary>
    /// <param name="title">The schema title.</param>
    /// <param name="properties">The member constraints keyed by member name.</param>
    public ValidationObjectSchema(string title, IReadOnlyDictionary<string, ValidationSchemaConstraints> properties) {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(properties);

        Title = title;
        Properties = properties;
    }

    /// <summary>
    /// Gets the schema title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the member constraints keyed by member name.
    /// </summary>
    public IReadOnlyDictionary<string, ValidationSchemaConstraints> Properties { get; }
}
