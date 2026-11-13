//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.OpenApi;

/// <summary>
/// Applies mapped validation constraints to an OpenAPI schema.
/// </summary>
public static class ValidationSchemaApplier {
    /// <summary>
    /// Applies the constraints to the supplied schema.
    /// </summary>
    /// <param name="constraints">The mapped constraints.</param>
    /// <param name="schema">The OpenAPI schema to modify.</param>
    /// <exception cref="ArgumentNullException"><paramref name="constraints"/> or <paramref name="schema"/> is null.</exception>
    public static void Apply(ValidationSchemaConstraints constraints, OpenApiSchema schema) =>
        Apply(constraints, schema, options: null);

    /// <summary>
    /// Applies the constraints to the supplied schema using the serializer options for nested names.
    /// </summary>
    /// <param name="constraints">The mapped constraints.</param>
    /// <param name="schema">The OpenAPI schema to modify.</param>
    /// <param name="options">The serializer options, when nested member names must be resolved.</param>
    /// <exception cref="ArgumentNullException"><paramref name="constraints"/> or <paramref name="schema"/> is null.</exception>
    public static void Apply(
        ValidationSchemaConstraints constraints,
        OpenApiSchema schema,
        JsonSerializerOptions? options) {
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(schema);

        Apply(constraints, schema, [], options);
    }

    private static void Apply(
        ValidationSchemaConstraints constraints,
        OpenApiSchema schema,
        HashSet<Type> visitedTypes,
        JsonSerializerOptions? options) {
        ApplyType(constraints, schema);
        ApplyScalarConstraints(constraints, schema);
        ApplyFormatConstraints(constraints, schema);
        ApplyEnumConstraints(constraints, schema);
        ApplyDescriptorConstraints(constraints, schema, visitedTypes, options);
    }

    private static void ApplyType(ValidationSchemaConstraints constraints, OpenApiSchema schema) =>
        schema.Type = constraints.Type switch {
            "string" => JsonSchemaType.String,
            "number" => JsonSchemaType.Number,
            "array" => JsonSchemaType.Array,
            "object" => JsonSchemaType.Object,
            _ => schema.Type
        };

    private static void ApplyScalarConstraints(ValidationSchemaConstraints constraints, OpenApiSchema schema) {
        if(constraints.MinLength is int minLength) {
            schema.MinLength = minLength;
        }

        if(constraints.MaxLength is int maxLength) {
            schema.MaxLength = maxLength;
        }

        if(constraints.Pattern is { } pattern) {
            schema.Pattern = pattern;
        }

        if(constraints.Minimum is decimal minimum) {
            schema.Minimum = minimum.ToString(CultureInfo.InvariantCulture);
        }

        if(constraints.Maximum is decimal maximum) {
            schema.Maximum = maximum.ToString(CultureInfo.InvariantCulture);
        }

        if(constraints.ExclusiveMinimum is decimal exclusiveMinimum) {
            schema.ExclusiveMinimum = exclusiveMinimum.ToString(CultureInfo.InvariantCulture);
        }

        if(constraints.ExclusiveMaximum is decimal exclusiveMaximum) {
            schema.ExclusiveMaximum = exclusiveMaximum.ToString(CultureInfo.InvariantCulture);
        }

        if(constraints.MinItems is int minItems) {
            schema.MinItems = minItems;
        }

        if(constraints.MaxItems is int maxItems) {
            schema.MaxItems = maxItems;
        }

        if(constraints.MultipleOf is decimal multipleOf) {
            schema.MultipleOf = multipleOf;
        }

        if(constraints.UniqueItems is bool uniqueItems) {
            schema.UniqueItems = uniqueItems;
        }
    }

    private static void ApplyFormatConstraints(ValidationSchemaConstraints constraints, OpenApiSchema schema) {
        schema.Format = constraints.Format ??
                        (string.Equals(constraints.ContentEncoding, "base64", StringComparison.Ordinal)
                            ? "byte"
                            : schema.Format);

        if(constraints.AnyOfFormats is { Count: > 0 }) {
            schema.AnyOf = [.. constraints.AnyOfFormats.Select(static format => new OpenApiSchema {
                Type = JsonSchemaType.String,
                Format = format
            })];
        }
    }

    private static void ApplyEnumConstraints(ValidationSchemaConstraints constraints, OpenApiSchema schema) {
        if(constraints.Enum is { Count: > 0 }) {
            schema.Enum = [.. constraints.Enum.Select(static value => (JsonNode)JsonValue.Create(value))];
        }

        if(constraints.EnumType is not null) {
            schema.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
            schema.Extensions["x-enum-type"] = new JsonNodeExtension(JsonValue.Create(constraints.EnumType));
        }
    }

    private static void ApplyDescriptorConstraints(
        ValidationSchemaConstraints constraints,
        OpenApiSchema schema,
        HashSet<Type> visitedTypes,
        JsonSerializerOptions? options) {
        if(constraints.ItemDescriptor is { } item) {
            var itemSchema = new OpenApiSchema();
            ApplyDescriptor(item, itemSchema, visitedTypes, options);
            schema.Items = itemSchema;
        } else if(constraints.NestedDescriptor is { } nested) {
            ApplyDescriptor(nested, schema, visitedTypes, options);
        }
    }

    private static void ApplyDescriptor(
        IValidationDescriptor descriptor,
        OpenApiSchema schema,
        HashSet<Type> visitedTypes,
        JsonSerializerOptions? options) {
        if(descriptor.ValidatedType is { } type && !visitedTypes.Add(type)) {
            return;
        }

        var mapped = ValidationSchemaMapper.Map(descriptor);
        schema.Type = JsonSchemaType.Object;
        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();

        foreach(var property in mapped.Properties) {
            var propertySchema = new OpenApiSchema();
            Apply(property.Value, propertySchema, [.. visitedTypes], options);

            var serializedName = JsonMemberNameMap.Resolve(options, descriptor.ValidatedType, property.Key)
                                 ?? property.Key;
            schema.Properties[serializedName] = propertySchema;

            if(IsRequired(property.Value)) {
                schema.Required ??= new HashSet<string>(StringComparer.Ordinal);
                schema.Required.Add(serializedName);
            }
        }
    }

    private static bool IsRequired(ValidationSchemaConstraints constraints) {
        if(!constraints.IsRequired) {
            return false;
        }

        var memberType = constraints.MemberType;
        if(memberType is null) {
            return true;
        }

        return Nullable.GetUnderlyingType(memberType) is not null || !memberType.IsValueType;
    }
}
