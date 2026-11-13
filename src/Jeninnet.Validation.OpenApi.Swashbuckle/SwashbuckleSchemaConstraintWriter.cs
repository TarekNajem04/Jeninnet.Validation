//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.OpenApi.Swashbuckle;

/// <summary>
/// Writes validated constraints onto a Swashbuckle schema. The descriptor-to-constraint mapping
/// is owned by <see cref="ValidationSchemaMapper"/>; this writer only translates the result to
/// the Swashbuckle target types (Microsoft.OpenApi 2.x) and leaves existing values untouched
/// when the validator has no opinion.
/// </summary>
internal static class SwashbuckleSchemaConstraintWriter {
    internal static void Apply(ValidationSchemaConstraints constraints, OpenApiSchema schema) {
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(schema);

        if(constraints.Type is not null && schema.Type is null) {
            schema.Type = constraints.Type switch {
                "string" => JsonSchemaType.String,
                "number" => JsonSchemaType.Number,
                "array" => JsonSchemaType.Array,
                "object" => JsonSchemaType.Object,
                _ => schema.Type
            };
        }

        SchemaConstraintWriter.Apply(constraints, new SwashbuckleConstraintTarget(schema));

        if(constraints.AnyOfFormats is { Count: > 0 } formats) {
            schema.AnyOf = [.. formats
                .Select(static item => (IOpenApiSchema)new OpenApiSchema {
                    Type = JsonSchemaType.String,
                    Format = item
                })];
        }

        if(constraints.Enum is { Count: > 0 } values) {
            schema.Enum = [.. values.Select(static value => (JsonNode)JsonValue.Create(value))];
        }

        if(constraints.EnumType is not null) {
            schema.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
            schema.Extensions["x-enum-type"] = new JsonNodeExtension(JsonValue.Create(constraints.EnumType));
        }
    }

    private sealed class SwashbuckleConstraintTarget(OpenApiSchema schema) : ISchemaConstraintTarget {
        public void SetMinLength(int value) => schema.MinLength = value;

        public void SetMaxLength(int value) => schema.MaxLength = value;

        public void SetPattern(string value) => schema.Pattern = value;

        public void SetMinimum(decimal value) =>
            schema.Minimum = value.ToString(CultureInfo.InvariantCulture);

        public void SetMaximum(decimal value) =>
            schema.Maximum = value.ToString(CultureInfo.InvariantCulture);

        public void SetExclusiveMinimum(decimal value) =>
            schema.ExclusiveMinimum = value.ToString(CultureInfo.InvariantCulture);

        public void SetExclusiveMaximum(decimal value) =>
            schema.ExclusiveMaximum = value.ToString(CultureInfo.InvariantCulture);

        public void SetMinItems(int value) => schema.MinItems = value;

        public void SetMaxItems(int value) => schema.MaxItems = value;

        public void SetUniqueItems(bool value) => schema.UniqueItems = value;

        public void SetMultipleOf(decimal value) => schema.MultipleOf = value;

        public void SetFormat(string value) => schema.Format = value;
    }
}
