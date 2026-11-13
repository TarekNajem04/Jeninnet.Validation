//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi;

/// <summary>
/// Applies the scalar constraints shared by the OpenAPI adapter writers. Target-specific
/// type mapping, <c>anyOf</c>, enumeration, and extension handling stay in each adapter.
/// </summary>
internal static class SchemaConstraintWriter {
    internal static void Apply(ValidationSchemaConstraints constraints, ISchemaConstraintTarget target) {
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(target);

        if(constraints.MinLength is { } minLength) {
            target.SetMinLength(minLength);
        }

        if(constraints.MaxLength is { } maxLength) {
            target.SetMaxLength(maxLength);
        }

        if(constraints.Pattern is { } pattern) {
            target.SetPattern(pattern);
        }

        if(constraints.Minimum is { } minimum) {
            target.SetMinimum(minimum);
        }

        if(constraints.Maximum is { } maximum) {
            target.SetMaximum(maximum);
        }

        if(constraints.ExclusiveMinimum is { } exclusiveMinimum) {
            target.SetExclusiveMinimum(exclusiveMinimum);
        }

        if(constraints.ExclusiveMaximum is { } exclusiveMaximum) {
            target.SetExclusiveMaximum(exclusiveMaximum);
        }

        if(constraints.MinItems is { } minItems) {
            target.SetMinItems(minItems);
        }

        if(constraints.MaxItems is { } maxItems) {
            target.SetMaxItems(maxItems);
        }

        if(constraints.UniqueItems is { } uniqueItems) {
            target.SetUniqueItems(uniqueItems);
        }

        if(constraints.MultipleOf is { } multipleOf) {
            target.SetMultipleOf(multipleOf);
        }

        var format = constraints.Format ??
                     (string.Equals(constraints.ContentEncoding, "base64", StringComparison.Ordinal)
                         ? "byte"
                         : null);

        if(format is not null) {
            target.SetFormat(format);
        }
    }
}
