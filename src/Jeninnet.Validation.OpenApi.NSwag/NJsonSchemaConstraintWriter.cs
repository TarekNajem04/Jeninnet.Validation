//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi.NSwag;

/// <summary>
/// Writes validated constraints onto an NJsonSchema schema. The descriptor-to-constraint mapping
/// is owned by <see cref="ValidationSchemaMapper"/>; this writer only translates the result to
/// NJsonSchema types and leaves existing values untouched when the validator has no opinion.
/// </summary>
internal static class NJsonSchemaConstraintWriter {
    internal static void Apply(ValidationSchemaConstraints constraints, NJsonSchemaType schema) {
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(schema);

        if(constraints.Type is not null && schema.Type == JsonObjectType.None) {
            schema.Type = constraints.Type switch {
                "string" => JsonObjectType.String,
                "number" => JsonObjectType.Number,
                "array" => JsonObjectType.Array,
                "object" => JsonObjectType.Object,
                _ => JsonObjectType.None
            };
        }

        SchemaConstraintWriter.Apply(constraints, new NJsonSchemaConstraintTarget(schema));

        if(constraints.AnyOfFormats is { Count: > 0 } formats) {
            schema.AnyOf.Clear();

            foreach(var item in formats) {
                schema.AnyOf.Add(new NJsonSchemaType {
                    Type = JsonObjectType.String,
                    Format = item
                });
            }
        }

        if(constraints.Enum is { Count: > 0 } values) {
            foreach(var value in values.Where(value => !schema.Enumeration.Contains(value))) {
                schema.Enumeration.Add(value);
            }
        }

        if(constraints.EnumType is not null) {
            schema.ExtensionData ??= new Dictionary<string, object?>(StringComparer.Ordinal);
            schema.ExtensionData["x-enum-type"] = constraints.EnumType;
        }
    }

    private sealed class NJsonSchemaConstraintTarget(NJsonSchemaType schema) : ISchemaConstraintTarget {
        public void SetMinLength(int value) => schema.MinLength = value;

        public void SetMaxLength(int value) => schema.MaxLength = value;

        public void SetPattern(string value) => schema.Pattern = value;

        public void SetMinimum(decimal value) => schema.Minimum = value;

        public void SetMaximum(decimal value) => schema.Maximum = value;

        public void SetExclusiveMinimum(decimal value) {
            schema.ExclusiveMinimum = value;
            schema.IsExclusiveMinimum = true;
        }

        public void SetExclusiveMaximum(decimal value) {
            schema.ExclusiveMaximum = value;
            schema.IsExclusiveMaximum = true;
        }

        public void SetMinItems(int value) => schema.MinItems = value;

        public void SetMaxItems(int value) => schema.MaxItems = value;

        public void SetUniqueItems(bool value) => schema.UniqueItems = value;

        public void SetMultipleOf(decimal value) => schema.MultipleOf = value;

        public void SetFormat(string value) => schema.Format = value;
    }
}
